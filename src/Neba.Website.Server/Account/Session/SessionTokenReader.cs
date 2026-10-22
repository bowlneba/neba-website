using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Neba.Website.Server.Account.Session;

internal static class SessionTokenReader
{
    /// <summary>
    /// Reads the tokens from the auth cookie on <paramref name="httpContext"/>, or null when the
    /// request isn't signed in.
    /// </summary>
    public static async Task<SessionTokens?> ReadAsync(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return null;

        var accessToken = await httpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, SecurityClaimsBuilder.AccessTokenName);
        var refreshToken = await httpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, SecurityClaimsBuilder.RefreshTokenName);

        if (accessToken is null || refreshToken is null)
            return null;

        var email = httpContext.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        return new SessionTokens(userId, email, accessToken, refreshToken);
    }
}