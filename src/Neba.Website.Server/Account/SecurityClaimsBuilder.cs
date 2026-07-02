using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using Neba.Api.Contracts.Security;

namespace Neba.Website.Server.Account;

internal static class SecurityClaimsBuilder
{
    public const string AccessTokenName = "access_token";
    public const string RefreshTokenName = "refresh_token";

    public static ClaimsPrincipal BuildPrincipal(string accessToken, string userId, string email)
    {
        var identity = new ClaimsIdentity(
            BuildClaims(accessToken, userId, email),
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identity);
    }

    // Tokens are credential material, not identity claims — kept out of ClaimsPrincipal.Claims
    // (where arbitrary code might enumerate/display them) and stored via the framework's
    // purpose-built AuthenticationProperties token store instead.
    public static AuthenticationProperties BuildAuthenticationProperties(string accessToken, string refreshToken)
    {
        var properties = new AuthenticationProperties();

        properties.StoreTokens(
        [
            new AuthenticationToken { Name = AccessTokenName, Value = accessToken },
            new AuthenticationToken { Name = RefreshTokenName, Value = refreshToken },
        ]);

        return properties;
    }

    private static List<Claim> BuildClaims(string accessToken, string userId, string email)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
        };

        foreach (var jwtClaim in ParseJwtPayload(accessToken))
        {
            // Pick up role claims and any custom claims (e.g. usbc_id) from the JWT
            if (jwtClaim.Type == ClaimTypes.Role
                || jwtClaim.Type == "usbc_id"
                || jwtClaim.Type == Permissions.ClaimType)
            {
                claims.Add(jwtClaim);
            }
        }

        return claims;
    }

    private static IEnumerable<Claim> ParseJwtPayload(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3) yield break;

        // base64url → standard base64
        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        var padding = (4 - (payload.Length % 4)) % 4;
        if (padding < 4)
            payload += new string('=', padding);

        string json;
        try
        {
            json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        }
        catch (FormatException)
        {
            yield break;
        }

        using var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.Value.EnumerateArray())
                    yield return new Claim(prop.Name, item.GetString() ?? string.Empty);
            }
            else
            {
                yield return new Claim(prop.Name, prop.Value.ToString());
            }
        }
    }
}