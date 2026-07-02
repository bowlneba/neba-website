using System.Net;
using System.Security.Claims;

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

    private IRenderedComponent<Neba.Website.Server.Account.Logout.Logout> RenderLogout(HttpContext httpContext)
        => _ctx.Render<Neba.Website.Server.Account.Logout.Logout>(p => p.AddCascadingValue(httpContext));

    private DefaultHttpContext BuildAuthenticatedHttpContext(string? accessToken)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([], CookieAuthenticationDefaults.AuthenticationScheme));

        var tokens = new List<AuthenticationToken>();
        if (accessToken is not null)
            tokens.Add(new AuthenticationToken { Name = SecurityClaimsBuilder.AccessTokenName, Value = accessToken });

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
