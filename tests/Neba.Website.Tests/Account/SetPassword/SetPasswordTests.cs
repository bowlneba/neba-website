using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

using SetPasswordPage = Neba.Website.Server.Account.SetPassword.SetPassword;

namespace Neba.Website.Tests.Account.SetPassword;

[UnitTest]
[Component("Website.Account.SetPassword")]
public sealed class SetPasswordTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<ISecurityApi> _mockApi;

    public SetPasswordTests()
    {
        _mockApi = new Mock<ISecurityApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(sp => new ApiExecutor(mockStopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should not call the API when passwords do not match")]
    public async Task Submit_ShouldNotCallApi_WhenPasswordsDoNotMatch()
    {
        // Arrange
        var cut = RenderSetPassword();
        await cut.InvokeAsync(() => cut.FindAll("input[type=password]")[0].Input("GoodPassword1"));
        await cut.InvokeAsync(() => cut.FindAll("input[type=password]")[1].Input("Different1"));

        // Act
        var submitButton = cut.Find("button[type=submit]");

        // Assert
        submitButton.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact(DisplayName = "Should navigate to the login page with passwordSet=1 when set-password succeeds")]
    public async Task Submit_ShouldNavigateToLoginWithPasswordSetFlag_WhenSucceeds()
    {
        // Arrange
        using var response = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true
        };

        _mockApi
            .Setup(api => api.SetPasswordFromTokenAsync(It.IsAny<Neba.Api.Contracts.Security.SetPasswordFromToken.SetPasswordFromTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var cut = RenderSetPassword();
        await cut.InvokeAsync(() => cut.FindAll("input[type=password]")[0].Input("GoodPassword1"));
        await cut.InvokeAsync(() => cut.FindAll("input[type=password]")[1].Input("GoodPassword1"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldContain("/account/login");
        nav.Uri.ShouldContain("passwordSet=true");
    }

    [Fact(DisplayName = "Should show an inline error and stay on the page when the token is invalid or expired")]
    public async Task Submit_ShouldShowInlineError_WhenTokenInvalidOrExpired()
    {
        // Arrange
        using var response = new StubApiResponse<object>
        {
            IsSuccessStatusCode = false
        };

        _mockApi
            .Setup(api => api.SetPasswordFromTokenAsync(It.IsAny<Neba.Api.Contracts.Security.SetPasswordFromToken.SetPasswordFromTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var cut = RenderSetPassword();
        await cut.InvokeAsync(() => cut.FindAll("input[type=password]")[0].Input("GoodPassword1"));
        await cut.InvokeAsync(() => cut.FindAll("input[type=password]")[1].Input("GoodPassword1"));
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var originalUri = nav.Uri;

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("invalid or has expired");
        nav.Uri.ShouldBe(originalUri);
    }

    [Theory(DisplayName = "Should show the reset or invite copy based on the reason in the link")]
    [InlineData("&reason=reset", "Choose a New Password")]
    [InlineData("&reason=invite", "Set Your Password")]
    [InlineData("", "Set Your Password")]
    [InlineData("&reason=bogus", "Set Your Password")]
    public void Render_ShouldShowCopyForLinkType(string reasonQuery, string heading)
    {
        // Arrange & Act
        var cut = RenderSetPassword(reasonQuery);

        // Assert
        cut.Find("h1").TextContent.ShouldBe(heading);
    }

    [Theory(DisplayName = "Should send Purpose only for a known reason")]
    [InlineData("&reason=reset", "reset")]
    [InlineData("&reason=invite", "invite")]
    [InlineData("&reason=bogus", null)]
    [InlineData("", null)]
    public async Task Submit_ShouldSendPurpose_OnlyForKnownReason(string reasonQuery, string? expected)
    {
        // Arrange
        Neba.Api.Contracts.Security.SetPasswordFromToken.SetPasswordFromTokenRequest? sent = null;
        using var response = new StubApiResponse<object> { IsSuccessStatusCode = true };
        _mockApi
            .Setup(api => api.SetPasswordFromTokenAsync(It.IsAny<Neba.Api.Contracts.Security.SetPasswordFromToken.SetPasswordFromTokenRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Neba.Api.Contracts.Security.SetPasswordFromToken.SetPasswordFromTokenRequest, CancellationToken>((request, _) => sent = request)
            .ReturnsAsync(response);
        var cut = RenderSetPassword(reasonQuery);
        await cut.InvokeAsync(() => cut.FindAll("input[type=password]")[0].Input("GoodPassword1"));
        await cut.InvokeAsync(() => cut.FindAll("input[type=password]")[1].Input("GoodPassword1"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        sent.ShouldNotBeNull();
        sent.Purpose.ShouldBe(expected);
    }

    [Fact(DisplayName = "Should offer a new link when a reset link is invalid or expired")]
    public async Task Submit_ShouldLinkToForgotPassword_WhenResetTokenInvalid()
    {
        // Arrange
        using var response = new StubApiResponse<object> { IsSuccessStatusCode = false };
        _mockApi
            .Setup(api => api.SetPasswordFromTokenAsync(It.IsAny<Neba.Api.Contracts.Security.SetPasswordFromToken.SetPasswordFromTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var cut = RenderSetPassword("&reason=reset");
        await cut.InvokeAsync(() => cut.FindAll("input[type=password]")[0].Input("GoodPassword1"));
        await cut.InvokeAsync(() => cut.FindAll("input[type=password]")[1].Input("GoodPassword1"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("This reset link is invalid or has expired.");
        cut.Find("a[href='/account/forgot-password']").TextContent.ShouldBe("Request a new link");
    }

    [Fact(DisplayName = "Should keep the invite message and offer no new-link link when an invite is invalid")]
    public async Task Submit_ShouldKeepInviteMessage_WhenInviteTokenInvalid()
    {
        // Arrange
        using var response = new StubApiResponse<object> { IsSuccessStatusCode = false };
        _mockApi
            .Setup(api => api.SetPasswordFromTokenAsync(It.IsAny<Neba.Api.Contracts.Security.SetPasswordFromToken.SetPasswordFromTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var cut = RenderSetPassword("&reason=invite");
        await cut.InvokeAsync(() => cut.FindAll("input[type=password]")[0].Input("GoodPassword1"));
        await cut.InvokeAsync(() => cut.FindAll("input[type=password]")[1].Input("GoodPassword1"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Ask an admin to send a new one.");
        cut.FindAll("a[href='/account/forgot-password']").ShouldBeEmpty();
    }

    private IRenderedComponent<SetPasswordPage> RenderSetPassword(string extraQuery = "")
    {
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/account/set-password?userId=test-user&token=valid-token" + extraQuery);

        return _ctx.Render<SetPasswordPage>();
    }
}