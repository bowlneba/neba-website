using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;

using Neba.Website.Server.Services;

namespace Neba.Website.Server.Account.Session;

/// <summary>
/// Lets a Blazor circuit read and rewrite the browser's auth cookie. A circuit can't set cookies
/// itself (its response started long ago), so <c>session-tokens.js</c> calls these endpoints from
/// the browser on its behalf. Tokens only travel as <see cref="SessionTokenProtector"/> blobs.
/// A blob stays valid for its short lifetime and can be replayed within it, so
/// <see cref="PersistTokensAsync"/> only lets it rewrite the cookie of the user it was issued for,
/// and never replaces the cookie's tokens with an older pair.
/// </summary>
internal static class SessionTokenEndpoints
{
    public const string Route = "/account/session-tokens";

    extension(WebApplication app)
    {
        public void MapSessionTokenEndpoints()
        {
            var group = app.MapGroup(Route).RequireAuthorization();

            group.MapGet(string.Empty, GetTokensAsync);
            group.MapPost(string.Empty, PersistTokensAsync);
        }
    }

    /// <summary>
    /// Returns the cookie's current tokens, so a circuit can pick up a refresh another tab already wrote.
    /// </summary>
    internal static async Task<Results<Ok<SessionTokenBlob>, NoContent>> GetTokensAsync(
        HttpContext httpContext,
        SessionTokenProtector protector)
    {
        httpContext.Response.Headers.CacheControl = "no-store";

        var tokens = await SessionTokenReader.ReadAsync(httpContext);

        return tokens is null
            ? TypedResults.NoContent()
            : TypedResults.Ok(new SessionTokenBlob(protector.Protect(tokens)));
    }

    /// <summary>Writes tokens a circuit just refreshed into the cookie.</summary>
    internal static async Task<Results<NoContent, BadRequest, StatusCodeHttpResult>> PersistTokensAsync(
        HttpContext httpContext,
        SessionTokenBlob body,
        SessionTokenProtector protector)
    {
        var tokens = protector.Unprotect(body.Blob);
        if (tokens is null)
            return TypedResults.BadRequest();

        // A blob may only update the session of the user it was issued for.
        if (tokens.UserId != httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier))
            return TypedResults.StatusCode(StatusCodes.Status403Forbidden);

        // A delayed or replayed blob must not roll the cookie back to an already-rotated refresh token.
        var current = await SessionTokenReader.ReadAsync(httpContext);
        if (current is not null && IsOlder(tokens, current))
            return TypedResults.NoContent();

        var principal = SecurityClaimsBuilder.BuildPrincipal(tokens.AccessToken, tokens.UserId, tokens.Email);
        var properties = SecurityClaimsBuilder.BuildAuthenticationProperties(tokens.AccessToken, tokens.RefreshToken);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);

        return TypedResults.NoContent();
    }

    private static bool IsOlder(SessionTokens candidate, SessionTokens current)
    {
        if (candidate.RefreshToken == current.RefreshToken)
            return false;

        var candidateExpiry = BearerTokenHandler.ReadExpiry(candidate.AccessToken);
        var currentExpiry = BearerTokenHandler.ReadExpiry(current.AccessToken);

        return candidateExpiry is { } candidateAt && currentExpiry is { } currentAt && candidateAt <= currentAt;
    }
}
