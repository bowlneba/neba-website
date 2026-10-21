using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using Neba.Api.Contracts.Security.RefreshToken;
using Neba.Website.Server.Account.Session;

namespace Neba.Website.Server.Services;

/// <summary>
/// A delegating handler that adds a bearer token to outgoing HTTP requests.
/// </summary>
/// <remarks>
/// <c>IHttpClientFactory</c> builds this handler in its own DI scope and shares it across every
/// user for the handler's lifetime, so it must hold no per-user state. Tokens come from an
/// <see cref="ITokenSession"/> chosen per call: the current circuit's when there is one
/// (<see cref="CircuitServicesAccessor"/>), otherwise the current HTTP request's cookie.
/// </remarks>
/// <param name="httpContextAccessor">
/// The HTTP context accessor. Inside a circuit this returns the HttpContext of the request that
/// opened the connection, whose cookie never changes, so it's only used to fill an empty circuit cache.
/// </param>
/// <param name="circuitServicesAccessor">The current circuit's services, or null outside a circuit.</param>
/// <param name="httpClientFactory">Used to build a plain HTTP client for the silent-refresh call (bypasses Refit to avoid a circular dependency on this same handler).</param>
/// <param name="apiConfiguration">The base URL of the security API.</param>
/// <param name="timeProvider">Clock used to decide whether a token is about to expire.</param>
/// <param name="logger">Logger for silent-refresh failures.</param>
internal sealed class BearerTokenHandler(
    IHttpContextAccessor httpContextAccessor,
    CircuitServicesAccessor circuitServicesAccessor,
    IHttpClientFactory httpClientFactory,
    NebaApiConfiguration apiConfiguration,
    TimeProvider timeProvider,
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

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = ResolveSession();
        string? token = null;

        if (session is not null)
        {
            await EnsureSeededAsync(session, cancellationToken);
            token = session.Cache.AccessToken;

            // Proactively refresh an expired (or about-to-expire) token instead of only reacting to a
            // 401 below. This matters for AllowAnonymous endpoints that read the caller's identity when
            // present (e.g. GetTournamentEndpoint's permission-gated fields) - they never return 401 for
            // a stale token, so without this the caller silently gets treated as anonymous instead of
            // getting a refreshed token.
            if (token is not null && IsExpiredOrExpiringSoon(token, session.Cache))
            {
                token = await RefreshWithLockAsync(session, token, cancellationToken) ?? token;
            }
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

        if (response.StatusCode == HttpStatusCode.Unauthorized && session is not null)
        {
            var refreshedToken = await RefreshWithLockAsync(session, token, cancellationToken);

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

    private ITokenSession? ResolveSession()
    {
        var httpContext = httpContextAccessor.HttpContext;
        var circuitServices = circuitServicesAccessor.Services;

        if (circuitServices is not null)
        {
            return new CircuitTokenSession(
                circuitServices.GetRequiredService<SessionTokenCache>(),
                httpContext,
                circuitServices.GetRequiredService<ISessionCookieSync>());
        }

        return httpContext is null ? null : new CookieTokenSession(httpContext);
    }

    // Under the lock so a fill can't overwrite tokens a concurrent call has just refreshed.
    private static async Task EnsureSeededAsync(ITokenSession session, CancellationToken cancellationToken)
    {
        if (session.Cache.IsSeeded)
            return;

        await session.Cache.RefreshLock.WaitAsync(cancellationToken);

        try
        {
            if (!session.Cache.IsSeeded)
                session.Cache.Set(await session.ReadInitialTokensAsync(cancellationToken));
        }
        finally
        {
            session.Cache.RefreshLock.Release();
        }
    }

    /// <summary>
    /// Wraps <see cref="TryRefreshAsync"/> in <see cref="SessionTokenCache.RefreshLock"/> so a burst
    /// of concurrent calls in the same session serialize through one refresh instead of each racing
    /// the server with the same soon-to-be-rotated-out refresh token. <paramref name="tokenAtCallTime"/>
    /// is the token this caller saw before deciding it needed refreshing; if another caller already
    /// refreshed past it, that newer token is reused instead of hitting the network again.
    /// </summary>
    private async Task<string?> RefreshWithLockAsync(ITokenSession session, string? tokenAtCallTime, CancellationToken cancellationToken)
    {
        await session.Cache.RefreshLock.WaitAsync(cancellationToken);

        try
        {
            if (NewerUsableToken(session.Cache.AccessToken, tokenAtCallTime, session.Cache) is { } newer)
                return newer;

            if (await TryRefreshAsync(session, cancellationToken) is { } refreshed)
                return refreshed;

            // Another tab sharing this cookie may have already redeemed this refresh token and
            // written its new pair to the cookie. Pick that up instead of signing the user out.
            var latest = await session.ReadLatestCookieTokensAsync(cancellationToken);
            if (latest is null || latest.RefreshToken == session.Cache.RefreshToken)
                return null;

            session.Cache.Set(latest);

            return NewerUsableToken(latest.AccessToken, tokenAtCallTime, session.Cache)
                ?? await TryRefreshAsync(session, cancellationToken);
        }
        finally
        {
            session.Cache.RefreshLock.Release();
        }
    }

    private string? NewerUsableToken(string? candidate, string? tokenAtCallTime, SessionTokenCache cache)
        => candidate is not null && candidate != tokenAtCallTime && !IsExpiredOrExpiringSoon(candidate, cache)
            ? candidate
            : null;

    private async Task<string?> TryRefreshAsync(ITokenSession session, CancellationToken cancellationToken)
    {
        var refreshToken = session.Cache.RefreshToken;
        var userId = session.Cache.UserId;

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

        var tokens = new SessionTokens(refreshed.UserId, refreshed.Email, refreshed.AccessToken, refreshed.RefreshToken);

        // The cache is updated first so later calls in this session use the new pair even if the
        // cookie can't be rewritten below.
        session.Cache.Set(tokens);

        if (!await session.PersistAsync(tokens, cancellationToken))
            logger.LogSilentRefreshCookieSkipped();

        return refreshed.AccessToken;
    }

    /// <summary>
    /// Reads the "exp" claim out of a JWT's payload segment without validating the signature -
    /// signature/issuer/audience validation already happens server-side on every call; this is
    /// only ever used to decide whether it's worth attempting a proactive refresh before sending.
    /// Treats a token that can't be parsed as expired, so it falls through to a refresh attempt
    /// rather than being sent as-is.
    /// </summary>
    private bool IsExpiredOrExpiringSoon(string jwt, SessionTokenCache cache)
        => cache.ExpiryOf(jwt, ReadExpiry) is not { } expiresAt
            || expiresAt <= timeProvider.GetUtcNow().Add(ExpiryBuffer);

    internal static DateTimeOffset? ReadExpiry(string jwt)
    {
        var parts = jwt.Split('.');

        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payloadBytes = Base64UrlDecode(parts[1]);
            using var payload = JsonDocument.Parse(payloadBytes);

            if (!payload.RootElement.TryGetProperty("exp", out var expElement))
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds(expElement.GetInt64());
        }
        catch (Exception ex) when (ex is FormatException or JsonException or InvalidOperationException)
        {
            return null;
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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Silent token refresh succeeded but the auth cookie could not be updated.")]
    public static partial void LogSilentRefreshCookieSkipped(this ILogger<BearerTokenHandler> logger);
}