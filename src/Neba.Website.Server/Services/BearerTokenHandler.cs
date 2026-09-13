using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using Neba.Api.Contracts.Security.RefreshToken;
using Neba.Website.Server.Account;

namespace Neba.Website.Server.Services;

/// <summary>
/// A delegating handler that adds a bearer token to outgoing HTTP requests.
/// </summary>
/// <param name="httpContextAccessor">The HTTP context accessor. Only populated for the duration of an actual HTTP request — null for calls made purely over an established SignalR circuit (e.g. from a <c>prerender: false</c> component), in which case <paramref name="tokenCache"/> is used instead.</param>
/// <param name="tokenCache">Circuit-scoped fallback token cache, seeded once per circuit by <c>Routes.razor</c>.</param>
/// <param name="httpClientFactory">Used to build a plain HTTP client for the silent-refresh call (bypasses Refit to avoid a circular dependency on this same handler).</param>
/// <param name="apiConfiguration">The base URL of the security API.</param>
/// <param name="logger">Logger for silent-refresh failures.</param>
internal sealed class BearerTokenHandler(
    IHttpContextAccessor httpContextAccessor,
    CircuitTokenCache tokenCache,
    IHttpClientFactory httpClientFactory,
    NebaApiConfiguration apiConfiguration,
    ILogger<BearerTokenHandler> logger)
    : DelegatingHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Buffer subtracted from the token's own "exp" claim so a proactive refresh happens slightly
    // before expiry rather than racing it - avoids sending a token that expires mid-flight.
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(30);

    // HttpClientFactory reuses a handler instance across many outgoing calls (default ~2 minute
    // handler lifetime), and a single Blazor request/circuit turn commonly fires several downstream
    // API calls with the same unchanged token. Caching just the last decode avoids re-parsing the
    // same JWT payload on every one of those calls; a wrong cache hit under concurrent access from a
    // different token just falls through to a fresh decode, so no locking is needed.
    private TokenExpiryCacheEntry? _lastExpiryCheck;

    private sealed record TokenExpiryCacheEntry(string Token, bool IsExpiredOrExpiringSoon);

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpContext = httpContextAccessor.HttpContext;
        var token = httpContext is not null
            ? await httpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, SecurityClaimsBuilder.AccessTokenName)
            : tokenCache.AccessToken;

        // Proactively refresh an expired (or about-to-expire) token instead of only reacting to a
        // 401 below. This matters for AllowAnonymous endpoints that read the caller's identity when
        // present (e.g. GetTournamentEndpoint's permission-gated fields) - they never return 401 for
        // a stale token, so without this the caller silently gets treated as anonymous instead of
        // getting a refreshed token. It also avoids an extra round trip on authenticated endpoints
        // for the common case of a circuit that's simply been open longer than the token's lifetime.
        if (token is not null && IsExpiredOrExpiringSoon(token))
        {
            token = await RefreshWithLockAsync(httpContext, token, cancellationToken) ?? token;
        }

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // Buffer the body before the first send so a 401 retry can replay it below via
        // CloneRequestAsync. Without this, content backed by a single-read stream (e.g. a
        // multipart file upload wrapping IBrowserFile.OpenReadStream()) is already fully drained
        // by this first send, and the retry would throw ("stream was already consumed") or send
        // an empty/corrupt body.
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync(cancellationToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var refreshedToken = await RefreshWithLockAsync(httpContext, token, cancellationToken);

            if (refreshedToken is not null)
            {
                response.Dispose();

                using var retryRequest = await CloneRequestAsync(request, cancellationToken);
                retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedToken);

                response = await base.SendAsync(retryRequest, cancellationToken);
            }
        }

        return response;
    }

    /// <summary>
    /// Wraps <see cref="TryRefreshAsync"/> in <see cref="CircuitTokenCache.RefreshLock"/> so a burst
    /// of concurrent calls on the same circuit (e.g. several near-simultaneous uploads whose access
    /// tokens all go stale together) serialize through one refresh instead of each racing the server
    /// with the same soon-to-be-rotated-out refresh token. <paramref name="tokenAtCallTime"/> is the
    /// token this caller observed before deciding it needed refreshing; if another caller already
    /// refreshed past that token by the time this one gets the lock, that newer token is reused
    /// instead of hitting the network again.
    /// </summary>
    private async Task<string?> RefreshWithLockAsync(HttpContext? httpContext, string? tokenAtCallTime, CancellationToken cancellationToken)
    {
        await tokenCache.RefreshLock.WaitAsync(cancellationToken);

        try
        {
            var latest = httpContext is not null
                ? await httpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, SecurityClaimsBuilder.AccessTokenName)
                : tokenCache.AccessToken;

            if (latest is not null && latest != tokenAtCallTime && !IsExpiredOrExpiringSoon(latest))
            {
                return latest;
            }

            return await TryRefreshAsync(httpContext, cancellationToken);
        }
        finally
        {
            tokenCache.RefreshLock.Release();
        }
    }

    private async Task<string?> TryRefreshAsync(HttpContext? httpContext, CancellationToken cancellationToken)
    {
        var refreshToken = httpContext is not null
            ? await httpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, SecurityClaimsBuilder.RefreshTokenName)
            : tokenCache.RefreshToken;
        var userId = httpContext is not null
            ? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : tokenCache.UserId;

        if (refreshToken is null || userId is null)
            return null;

        RefreshTokenResponse? refreshed;
        try
        {
            using var client = httpClientFactory.CreateClient();
            using var refreshResponse = await client.PostAsJsonAsync(
                new Uri(apiConfiguration.BaseUrl, "/security/refresh"),
                new RefreshTokenRequest { UserId = userId, RefreshToken = refreshToken },
                JsonOptions,
                cancellationToken);

            if (!refreshResponse.IsSuccessStatusCode)
                return null;

            refreshed = await refreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException)
        {
            logger.LogSilentRefreshFailed(ex);
            return null;
        }

        if (refreshed is null)
            return null;

        // Keep the circuit-scoped cache current regardless of whether the cookie itself can be
        // rewritten below, so later HttpContext-less calls on this circuit keep using a live token.
        tokenCache.AccessToken = refreshed.AccessToken;
        tokenCache.RefreshToken = refreshed.RefreshToken;

        if (httpContext?.Response.HasStarted != false)
        {
            if (httpContext is not null)
                logger.LogSilentRefreshCookieSkipped();

            return refreshed.AccessToken;
        }

        var principal = SecurityClaimsBuilder.BuildPrincipal(refreshed.AccessToken, refreshed.UserId, refreshed.Email);
        var properties = SecurityClaimsBuilder.BuildAuthenticationProperties(refreshed.AccessToken, refreshed.RefreshToken);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);

        return refreshed.AccessToken;
    }

    /// <summary>
    /// Reads the "exp" claim out of a JWT's payload segment without validating the signature -
    /// signature/issuer/audience validation already happens server-side on every call; this is
    /// only ever used to decide whether it's worth attempting a proactive refresh before sending.
    /// Treats a token that can't be parsed as expired, so it falls through to a refresh attempt
    /// rather than being sent as-is.
    /// </summary>
    private bool IsExpiredOrExpiringSoon(string jwt)
    {
        var cached = _lastExpiryCheck;
        if (cached is not null && cached.Token == jwt)
        {
            return cached.IsExpiredOrExpiringSoon;
        }

        var result = ComputeIsExpiredOrExpiringSoon(jwt);
        _lastExpiryCheck = new TokenExpiryCacheEntry(jwt, result);
        return result;
    }

    private static bool ComputeIsExpiredOrExpiringSoon(string jwt)
    {
        var parts = jwt.Split('.');

        if (parts.Length < 2)
        {
            return true;
        }

        try
        {
            var payloadBytes = Base64UrlDecode(parts[1]);
            using var payload = JsonDocument.Parse(payloadBytes);

            if (!payload.RootElement.TryGetProperty("exp", out var expElement))
            {
                return true;
            }

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expElement.GetInt64());
            return expiresAt <= DateTimeOffset.UtcNow.Add(ExpiryBuffer);
        }
        catch (Exception ex) when (ex is FormatException or JsonException or InvalidOperationException)
        {
            return true;
        }
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');

        return Convert.FromBase64String(padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '='));
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(bytes);

            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var option in request.Options)
            clone.Options.TryAdd(option.Key, option.Value);

        return clone;
    }
}

internal static partial class BearerTokenHandlerLogMessages
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Silent token refresh failed.")]
    public static partial void LogSilentRefreshFailed(this ILogger<BearerTokenHandler> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Silent token refresh succeeded but the auth cookie could not be updated because the response had already started.")]
    public static partial void LogSilentRefreshCookieSkipped(this ILogger<BearerTokenHandler> logger);
}