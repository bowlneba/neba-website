using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account;
using Neba.Website.Server.Account.Session;

namespace Neba.Website.Tests.Account.Session;

[UnitTest]
[Component("Website.Account.Session")]
public sealed class SessionTokenEndpointsTests
{
    private readonly SessionTokenProtector _protector = new(new EphemeralDataProtectionProvider(), NullLogger<SessionTokenProtector>.Instance);

    [Fact(DisplayName = "GET should return the cookie's tokens as a protected blob")]
    public async Task GetTokensAsync_ShouldReturnProtectedBlob_WhenCookieHasTokens()
    {
        // Arrange
        var httpContext = SessionHttpContext.Build(userId: "user-123", accessToken: "access-1", refreshToken: "refresh-1");

        // Act
        var result = await SessionTokenEndpoints.GetTokensAsync(httpContext, _protector);

        // Assert
        var ok = result.Result.ShouldBeOfType<Ok<SessionTokenBlob>>();
        _protector.Unprotect(ok.Value!.Blob).ShouldBe(new SessionTokens("user-123", "admin@bowlneba.com", "access-1", "refresh-1"));
        httpContext.Response.Headers.CacheControl.ToString().ShouldBe("no-store");
    }

    [Fact(DisplayName = "GET should return NoContent when the cookie holds no refresh token")]
    public async Task GetTokensAsync_ShouldReturnNoContent_WhenCookieHasNoTokens()
    {
        // Arrange
        var httpContext = SessionHttpContext.Build(userId: "user-123", accessToken: "access-1", refreshToken: null);

        // Act
        var result = await SessionTokenEndpoints.GetTokensAsync(httpContext, _protector);

        // Assert
        result.Result.ShouldBeOfType<NoContent>();
    }

    [Fact(DisplayName = "POST should rewrite the auth cookie with the blob's tokens")]
    public async Task PersistTokensAsync_ShouldSignIn_WhenBlobIsValidForCurrentUser()
    {
        // Arrange
        AuthenticationProperties? captured = null;
        var authServiceMock = new Mock<IAuthenticationService>(MockBehavior.Strict);
        authServiceMock
            .Setup(s => s.SignInAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
            .Callback<HttpContext, string, ClaimsPrincipal, AuthenticationProperties>((_, _, _, props) => captured = props)
            .Returns(Task.CompletedTask);
        var httpContext = SessionHttpContext.Build(userId: "user-123", accessToken: "access-1", refreshToken: "refresh-1", authServiceMock: authServiceMock);
        var blob = new SessionTokenBlob(_protector.Protect(new SessionTokens("user-123", "admin@bowlneba.com", "access-2", "refresh-2")));

        // Act
        var result = await SessionTokenEndpoints.PersistTokensAsync(httpContext, blob, _protector);

        // Assert
        result.Result.ShouldBeOfType<NoContent>();
        captured.ShouldNotBeNull();
        captured.GetTokenValue(SecurityClaimsBuilder.AccessTokenName).ShouldBe("access-2");
        captured.GetTokenValue(SecurityClaimsBuilder.RefreshTokenName).ShouldBe("refresh-2");
    }

    [Fact(DisplayName = "POST should return Forbidden when the blob belongs to a different user")]
    public async Task PersistTokensAsync_ShouldReturnForbidden_WhenBlobIsForAnotherUser()
    {
        // Arrange - strict auth mock with no SignInAsync setup: signing in would throw.
        var httpContext = SessionHttpContext.Build(userId: "user-123", accessToken: "access-1", refreshToken: "refresh-1");
        var blob = new SessionTokenBlob(_protector.Protect(new SessionTokens("someone-else", "other@bowlneba.com", "access-2", "refresh-2")));

        // Act
        var result = await SessionTokenEndpoints.PersistTokensAsync(httpContext, blob, _protector);

        // Assert
        result.Result.ShouldBeOfType<StatusCodeHttpResult>().StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact(DisplayName = "POST should return BadRequest when the blob can't be unprotected")]
    public async Task PersistTokensAsync_ShouldReturnBadRequest_WhenBlobIsInvalid()
    {
        // Arrange
        var httpContext = SessionHttpContext.Build(userId: "user-123", accessToken: "access-1", refreshToken: "refresh-1");

        // Act
        var result = await SessionTokenEndpoints.PersistTokensAsync(httpContext, new SessionTokenBlob("forged"), _protector);

        // Assert
        result.Result.ShouldBeOfType<BadRequest>();
    }
}