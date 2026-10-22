using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using Neba.Website.Server.Account;
using Neba.Website.Server.Account.Session;

namespace Neba.Website.Server.Services;

/// <summary>
/// Token session for calls made during a plain HTTP request (static rendering and prerendering).
/// Reads the request's auth cookie and rewrites it with <c>SignInAsync</c> while the response
/// hasn't started.
/// </summary>
internal sealed class CookieTokenSession(HttpContext httpContext) : ITokenSession
{
    public SessionTokenCache Cache { get; } = httpContext.RequestServices.GetRequiredService<SessionTokenCache>();

    public Task<SessionTokens?> ReadInitialTokensAsync(CancellationToken cancellationToken)
        => SessionTokenReader.ReadAsync(httpContext);

    // A request only ever sees the cookie it arrived with.
    public Task<SessionTokens?> ReadLatestCookieTokensAsync(CancellationToken cancellationToken)
        => Task.FromResult<SessionTokens?>(null);

    public async Task<bool> PersistAsync(SessionTokens tokens, CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
            return false;

        var principal = SecurityClaimsBuilder.BuildPrincipal(tokens.AccessToken, tokens.UserId, tokens.Email);
        var properties = SecurityClaimsBuilder.BuildAuthenticationProperties(tokens.AccessToken, tokens.RefreshToken);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);

        return true;
    }
}