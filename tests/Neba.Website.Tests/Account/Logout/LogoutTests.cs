using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Bunit;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account;
using Neba.Website.Server.Services;

namespace Neba.Website.Tests.Account.Logout;

[UnitTest]
[Component("Website.Account.Logout")]
public sealed class LogoutTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<IAuthenticationService> _authServiceMock;

    public LogoutTests()
    {
        _authServiceMock = new Mock<IAuthenticationService>(MockBehavior.Strict);

        _ctx = new BunitContext();
        _ctx.Services.AddSingleton(new NebaApiConfiguration { BaseUrl = new Uri("https://api.example.com") });
        _ctx.Services.AddSingleton(NullLogger<Neba.Website.Server.Account.Logout.Logout>.Instance);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should sign out, revoke the token server-side, and navigate home with loggedOut when revocation succeeds")]
    public void OnInitializedAsync_ShouldSignOutAndNavigateHome_WhenRevocationSucceeds()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        _ctx.Services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(innerHandler));

        var httpContext = BuildAuthenticatedHttpContext(accessToken: "old-token");

        // Act
        var cut = RenderLogout(httpContext);
        cut.WaitForState(() => innerHandler.Requests.Count == 1);

        // Assert
        _authServiceMock.VerifyAll();
        innerHandler.Requests.ShouldHaveSingleItem();
        innerHandler.Requests[0].RequestUri.ShouldBe(new Uri("https://api.example.com/security/logout"));
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "old-token"));
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        cut.WaitForAssertion(() => nav.Uri.ShouldContain("loggedOut=1"));
    }

    [Fact(DisplayName = "Should sign out and navigate home with logoutError when the revoke call returns a non-success status")]
    public void OnInitializedAsync_ShouldNavigateWithLogoutError_WhenRevokeCallFails()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        _ctx.Services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(innerHandler));

        var httpContext = BuildAuthenticatedHttpContext(accessToken: "old-token");

        // Act
        var cut = RenderLogout(httpContext);
        cut.WaitForState(() => innerHandler.Requests.Count == 1);

        // Assert
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        cut.WaitForAssertion(() => nav.Uri.ShouldContain("logoutError=1"));
    }

    [Fact(DisplayName = "Should sign out and navigate home with loggedOut without calling the API when there is no access token")]
    public void OnInitializedAsync_ShouldNavigateHomeWithoutApiCall_WhenNoAccessTokenPresent()
    {
        // Arrange
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        _ctx.Services.AddSingleton(factoryMock.Object);

        var httpContext = BuildAuthenticatedHttpContext(accessToken: null);

        // Act
        var cut = RenderLogout(httpContext);

        // Assert
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        cut.WaitForAssertion(() => nav.Uri.ShouldContain("loggedOut=1"));
        factoryMock.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Should navigate home with logoutError when the revoke request throws")]
    public void OnInitializedAsync_ShouldNavigateWithLogoutError_WhenRevokeRequestThrows()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => throw new HttpRequestException("network down"));
        _ctx.Services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(innerHandler));

        var httpContext = BuildAuthenticatedHttpContext(accessToken: "old-token");

        // Act
        var cut = RenderLogout(httpContext);

        // Assert
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        cut.WaitForAssertion(() => nav.Uri.ShouldContain("logoutError=1"));
    }

    [Fact(DisplayName = "Should refresh and retry the revoke call when the captured access token has expired, then navigate home with loggedOut")]
    public void OnInitializedAsync_ShouldRefreshAndRetryRevoke_WhenAccessTokenExpired()
    {
        // Arrange
        var logoutCallCount = 0;
        using var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/security/refresh")
            {
                var json = JsonSerializer.Serialize(new
                {
                    accessToken = "new-token",
                    refreshToken = "new-refresh",
                    expiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
                    userId = "user-123",
                    email = "admin@bowlneba.com",
                });

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }

            logoutCallCount++;
            return new HttpResponseMessage(logoutCallCount == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK);
        });

        _ctx.Services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(handler));

        var httpContext = BuildAuthenticatedHttpContext(accessToken: "old-token", refreshToken: "refresh-abc", userId: "user-123");

        // Act
        var cut = RenderLogout(httpContext);

        // Assert
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        cut.WaitForAssertion(() => nav.Uri.ShouldContain("loggedOut=1"));
        handler.Requests.Count(r => r.RequestUri!.AbsolutePath == "/security/logout").ShouldBe(2);
        handler.Requests.Count(r => r.RequestUri!.AbsolutePath == "/security/refresh").ShouldBe(1);
        handler.Requests.First(r => r.RequestUri!.AbsolutePath == "/security/logout" && r == handler.Requests[^1])
            .Headers.Authorization.ShouldBe(new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "new-token"));
    }

    [Fact(DisplayName = "Should navigate home with logoutError when the revoke call fails and there is no refresh token to fall back on")]
    public void OnInitializedAsync_ShouldNavigateWithLogoutError_WhenAccessTokenExpiredAndNoRefreshToken()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        _ctx.Services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(innerHandler));

        var httpContext = BuildAuthenticatedHttpContext(accessToken: "old-token");

        // Act
        var cut = RenderLogout(httpContext);

        // Assert
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        cut.WaitForAssertion(() => nav.Uri.ShouldContain("logoutError=1"));
        innerHandler.Requests.ShouldHaveSingleItem();
    }

    private IRenderedComponent<Neba.Website.Server.Account.Logout.Logout> RenderLogout(HttpContext httpContext)
        => _ctx.Render<Neba.Website.Server.Account.Logout.Logout>(p => p.AddCascadingValue(httpContext));

    private DefaultHttpContext BuildAuthenticatedHttpContext(string? accessToken, string? refreshToken = null, string? userId = null)
    {
        var claims = new List<Claim>();
        if (userId is not null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        var tokens = new List<AuthenticationToken>();
        if (accessToken is not null)
            tokens.Add(new AuthenticationToken { Name = SecurityClaimsBuilder.AccessTokenName, Value = accessToken });
        if (refreshToken is not null)
            tokens.Add(new AuthenticationToken { Name = SecurityClaimsBuilder.RefreshTokenName, Value = refreshToken });

        var properties = new AuthenticationProperties();
        properties.StoreTokens(tokens);

        var httpContext = new DefaultHttpContext { User = principal };

        _authServiceMock
            .Setup(s => s.AuthenticateAsync(httpContext, CookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(principal, properties, CookieAuthenticationDefaults.AuthenticationScheme)));
        _authServiceMock
            .Setup(s => s.SignOutAsync(httpContext, CookieAuthenticationDefaults.AuthenticationScheme, null))
            .Returns(Task.CompletedTask);

        httpContext.RequestServices = new ServiceCollection().AddSingleton(_authServiceMock.Object).BuildServiceProvider();

        return httpContext;
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }
}