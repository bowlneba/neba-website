using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account;
using Neba.Website.Server.Account.Session;
using Neba.Website.Server.Services;
using Neba.Website.Tests.Account.Session;

namespace Neba.Website.Tests.Services;

[UnitTest]
[Component("Website.Services.CookieTokenSession")]
public sealed class CookieTokenSessionTests
{
    private static readonly SessionTokens Tokens = new("user-123", "admin@bowlneba.com", "access-2", "refresh-2");

    [Fact(DisplayName = "Cache should be the request scope's SessionTokenCache")]
    public void Cache_ShouldResolveFromRequestServices()
    {
        // Arrange
        var cache = new SessionTokenCache();
        var httpContext = SessionHttpContext.Build("user-123", "access-1", "refresh-1", configureServices: s => s.AddSingleton(cache));

        // Act
        var sut = new CookieTokenSession(httpContext);

        // Assert
        sut.Cache.ShouldBeSameAs(cache);
    }

    [Fact(DisplayName = "ReadInitialTokensAsync should read the request's cookie")]
    public async Task ReadInitialTokensAsync_ShouldReadRequestCookie()
    {
        // Arrange
        var httpContext = SessionHttpContext.Build("user-123", "access-1", "refresh-1", configureServices: s => s.AddSingleton<SessionTokenCache>());
        var sut = new CookieTokenSession(httpContext);

        // Act
        var result = await sut.ReadInitialTokensAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(new SessionTokens("user-123", "admin@bowlneba.com", "access-1", "refresh-1"));
    }

    [Fact(DisplayName = "ReadLatestCookieTokensAsync should return null, since a request only sees the cookie it arrived with")]
    public async Task ReadLatestCookieTokensAsync_ShouldReturnNull()
    {
        // Arrange
        var httpContext = SessionHttpContext.Build("user-123", "access-1", "refresh-1", configureServices: s => s.AddSingleton<SessionTokenCache>());
        var sut = new CookieTokenSession(httpContext);

        // Act
        var result = await sut.ReadLatestCookieTokensAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "PersistAsync should sign in with the new tokens while the response hasn't started")]
    public async Task PersistAsync_ShouldSignIn_WhenResponseNotStarted()
    {
        // Arrange
        AuthenticationProperties? captured = null;
        var authServiceMock = new Mock<IAuthenticationService>(MockBehavior.Strict);
        authServiceMock
            .Setup(s => s.SignInAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
            .Callback<HttpContext, string, ClaimsPrincipal, AuthenticationProperties>((_, _, _, props) => captured = props)
            .Returns(Task.CompletedTask);
        var httpContext = SessionHttpContext.Build("user-123", "access-1", "refresh-1", authServiceMock, configureServices: s => s.AddSingleton<SessionTokenCache>());
        var sut = new CookieTokenSession(httpContext);

        // Act
        var result = await sut.PersistAsync(Tokens, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeTrue();
        captured.ShouldNotBeNull();
        captured.GetTokenValue(SecurityClaimsBuilder.AccessTokenName).ShouldBe("access-2");
        captured.GetTokenValue(SecurityClaimsBuilder.RefreshTokenName).ShouldBe("refresh-2");
    }

    [Fact(DisplayName = "PersistAsync should return false without signing in once the response has started")]
    public async Task PersistAsync_ShouldReturnFalse_WhenResponseStarted()
    {
        // Arrange - the strict auth mock has no SignInAsync setup, so signing in would throw.
        var httpContext = SessionHttpContext.Build("user-123", "access-1", "refresh-1", configureServices: s => s.AddSingleton<SessionTokenCache>());
        httpContext.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());
        var sut = new CookieTokenSession(httpContext);

        // Act
        var result = await sut.PersistAsync(Tokens, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeFalse();
    }

    private sealed class StartedResponseFeature : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }
}