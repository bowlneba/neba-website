using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using Neba.Api.Contracts.Security;

namespace Neba.Website.Server.Account;

/// <summary>
/// Test-only sign-in endpoint that issues a real cookie-auth session without going through the
/// real login flow, so Playwright e2e tests can exercise permission-gated UI (e.g. the article
/// delete controls) without a login flow the mock API doesn't implement. Only mapped in
/// Development — see <c>Program.cs</c>.
/// </summary>
internal static class TestAuthEndpoints
{
    public static void MapTestAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/__test/login", async (HttpContext httpContext, string? permissions, string? returnUrl) =>
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new(ClaimTypes.Email, "e2e-test-user@bowlneba.com"),
            };

            foreach (var permission in (permissions ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                claims.Add(new Claim(Permissions.ClaimType, permission));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return Results.Redirect(ReturnUrlValidator.GetSafeReturnUrl(returnUrl));
        });
    }
}
