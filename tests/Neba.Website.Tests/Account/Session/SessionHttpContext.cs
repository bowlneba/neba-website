using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Neba.Website.Server.Account;

namespace Neba.Website.Tests.Account.Session;

/// <summary>Builds an <see cref="HttpContext"/> whose auth cookie holds the given tokens.</summary>
internal static class SessionHttpContext
{
    public static DefaultHttpContext Build(
        string? userId,
        string? accessToken,
        string? refreshToken,
        Mock<IAuthenticationService>? authServiceMock = null,
        string? email = "admin@bowlneba.com",
        Action<IServiceCollection>? configureServices = null)
    {
        var claims = new List<Claim>();
        if (userId is not null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        if (email is not null)
            claims.Add(new Claim(ClaimTypes.Email, email));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        var tokens = new List<AuthenticationToken>();
        if (accessToken is not null)
            tokens.Add(new AuthenticationToken { Name = SecurityClaimsBuilder.AccessTokenName, Value = accessToken });
        if (refreshToken is not null)
            tokens.Add(new AuthenticationToken { Name = SecurityClaimsBuilder.RefreshTokenName, Value = refreshToken });

        var properties = new AuthenticationProperties();
        properties.StoreTokens(tokens);

        var httpContext = new DefaultHttpContext { User = principal };

        var auth = authServiceMock ?? new Mock<IAuthenticationService>(MockBehavior.Strict);
        auth.Setup(s => s.AuthenticateAsync(httpContext, CookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(principal, properties, CookieAuthenticationDefaults.AuthenticationScheme)));

        var services = new ServiceCollection();
        services.AddSingleton(auth.Object);
        configureServices?.Invoke(services);
        httpContext.RequestServices = services.BuildServiceProvider();

        return httpContext;
    }
}