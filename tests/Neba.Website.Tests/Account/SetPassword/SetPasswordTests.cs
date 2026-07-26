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
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
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
        cut.Find("button[type=submit]").ShouldSatisfyAllConditions(
            b => b.HasAttribute("disabled").ShouldBeTrue());

        // Assert
        _mockApi.Verify(
            api => api.SetPasswordFromTokenAsync(It.IsAny<Neba.Api.Contracts.Security.SetPasswordFromToken.SetPasswordFromTokenRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
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

    private IRenderedComponent<SetPasswordPage> RenderSetPassword()
    {
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/account/set-password?userId=test-user&token=valid-token");

        return _ctx.Render<SetPasswordPage>();
    }
}