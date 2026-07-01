using System.Net;
using System.Security.Claims;

using Bunit;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Security.Login;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;
using Neba.Website.Server.Account;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.Account.Login;

[UnitTest]
[Component("Website.Account.Login")]
public sealed class LoginTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<ISecurityApi> _mockApi;
    private readonly Mock<IAuthenticationService> _authServiceMock;

    public LoginTests()
    {
        _mockApi = new Mock<ISecurityApi>(MockBehavior.Strict);
        _authServiceMock = new Mock<IAuthenticationService>(MockBehavior.Strict);

        _ctx = new BunitContext();
        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(new AdminLoginSettings());
        _ctx.Services.AddSingleton(NullLogger<Neba.Website.Server.Account.Login.Login>.Instance);
        _ctx.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render the email and password fields")]
    public void Render_ShouldRenderEmailAndPasswordFields()
    {
        // Arrange & Act
        var cut = RenderLogin();

        // Assert
        cut.Find("#email").ShouldNotBeNull();
        cut.Find("#password").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should show validation messages when submitting an empty form")]
    public void Submit_ShouldShowValidationMessages_WhenFormIsEmpty()
    {
        // Arrange
        var cut = RenderLogin();

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.Markup.ShouldContain("Email is required.");
        cut.Markup.ShouldContain("Password is required.");
    }

    [Fact(DisplayName = "Should show an error message when credentials are invalid")]
    public void Submit_ShouldShowErrorMessage_WhenCredentialsAreInvalid()
    {
        // Arrange
        using var response = new StubApiResponse<LoginResponse>
        {
            IsSuccessStatusCode = false,
            Content = (LoginResponse?)null
        };

        _mockApi
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var cut = RenderLogin();
        FillForm(cut, "bowler@bowlneba.com", "WrongPassword1");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.Markup.ShouldContain("Invalid email or password.");
    }

    [Fact(DisplayName = "Should show a generic error message when the API call throws")]
    public void Submit_ShouldShowGenericErrorMessage_WhenApiCallThrows()
    {
        // Arrange
        _mockApi
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("network down"));

        var cut = RenderLogin();
        FillForm(cut, "bowler@bowlneba.com", "Password123");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.Markup.ShouldContain("Unable to reach the server.");
    }

    [Fact(DisplayName = "Should sign in and navigate home when credentials are valid")]
    public void Submit_ShouldSignInAndNavigateHome_WhenCredentialsAreValid()
    {
        // Arrange
        var loginResponse = LoginResponseFactory.Create(
            accessToken: BuildJwt(),
            refreshToken: "refresh-token",
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(15),
            userId: "user-1",
            email: "bowler@bowlneba.com");

        using var response = new StubApiResponse<LoginResponse>
        {
            IsSuccessStatusCode = true,
            Content = loginResponse
        };

        _mockApi
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _authServiceMock
            .Setup(s => s.SignInAsync(
                It.IsAny<HttpContext>(),
                Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                It.Is<ClaimsPrincipal>(p => p.FindFirst(ClaimTypes.NameIdentifier)!.Value == "user-1"),
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var cut = RenderLogin();
        FillForm(cut, "bowler@bowlneba.com", "Password123");

        // Act
        cut.Find("form").Submit();

        // Assert
        _authServiceMock.VerifyAll();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldEndWith("/");
    }

    private IRenderedComponent<Neba.Website.Server.Account.Login.Login> RenderLogin()
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(_authServiceMock.Object).BuildServiceProvider()
        };

        return _ctx.Render<Neba.Website.Server.Account.Login.Login>(p => p.AddCascadingValue(httpContext));
    }

    private static void FillForm(IRenderedComponent<Neba.Website.Server.Account.Login.Login> cut, string email, string password)
    {
        cut.Find("#email").Change(email);
        cut.Find("#password").Change(password);
    }

    private static string BuildJwt()
    {
        var header = Base64UrlEncode("""{"alg":"none","typ":"JWT"}"""u8.ToArray());
        var body = Base64UrlEncode("""{"sub":"user-1"}"""u8.ToArray());
        return $"{header}.{body}.signature";
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}