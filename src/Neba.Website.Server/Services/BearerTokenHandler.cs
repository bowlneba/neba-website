using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
/// <param name="httpContextAccessor">The HTTP context accessor.</param>
/// <param name="httpClientFactory">Used to build a plain HTTP client for the silent-refresh call (bypasses Refit to avoid a circular dependency on this same handler).</param>
/// <param name="apiConfiguration">The base URL of the security API.</param>
/// <param name="logger">Logger for silent-refresh failures.</param>
internal sealed class BearerTokenHandler(
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory httpClientFactory,
    NebaApiConfiguration apiConfiguration,
    ILogger<BearerTokenHandler> logger)
    : DelegatingHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpContext = httpContextAccessor.HttpContext;
        var token = httpContext?.User.FindFirst("access_token")?.Value;

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && httpContext is not null)
        {
            var refreshedToken = await TryRefreshAsync(httpContext, cancellationToken);

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

    private async Task<string?> TryRefreshAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var refreshToken = httpContext.User.FindFirst("refresh_token")?.Value;
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

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
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            logger.LogSilentRefreshFailed(ex);
            return null;
        }

        if (refreshed is null)
            return null;

        var principal = SecurityClaimsBuilder.BuildPrincipal(
            refreshed.AccessToken, refreshed.RefreshToken, refreshed.UserId, refreshed.Email);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return refreshed.AccessToken;
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
}
