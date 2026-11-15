using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Security.ForgotPassword;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;

using Refit.Testing;

using ForgotPasswordPage = Neba.Website.Server.Account.ForgotPassword.ForgotPassword;

namespace Neba.Website.Tests.Account.ForgotPassword;

[UnitTest]
[Component("Website.Account.ForgotPassword")]
public sealed class ForgotPasswordTests : IDisposable
{
    private const string SentMessage =
        "If an account exists for that email, we've sent a password reset link. The link expires in 1 hour.";

    private readonly BunitContext _ctx;
    private readonly Mock<ISecurityApi> _mockApi;

    public ForgotPasswordTests()
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

    private void SetupApi(StubApiResponse<object> response, Action<ForgotPasswordRequest>? capture = null)
        => _mockApi
            .Setup(api => api.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ForgotPasswordRequest, CancellationToken>((request, _) => capture?.Invoke(request))
            .ReturnsAsync(response);

    private static async Task SubmitAsync(IRenderedComponent<ForgotPasswordPage> cut, string email)
    {
        await cut.InvokeAsync(() => cut.Find("#email").Change(email));
        await cut.Find("form").SubmitAsync();
    }

    [Fact(DisplayName = "Should render the email field and a link back to log in")]
    public void Render_ShouldShowFormAndBackLink()
    {
        // Arrange & Act
        var cut = _ctx.Render<ForgotPasswordPage>();

        // Assert
        cut.Find("#email").ShouldNotBeNull();
        cut.Find("button[type=submit]").TextContent.Trim().ShouldBe("Send reset link");
        cut.Find("a[href='/account/login']").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should show the neutral message and hide the form when the request succeeds")]
    public async Task Submit_ShouldShowNeutralMessage_WhenApiSucceeds()
    {
        // Arrange
        using var response = new StubApiResponse<object> { IsSuccessStatusCode = true };
        SetupApi(response);
        var cut = _ctx.Render<ForgotPasswordPage>();

        // Act
        await SubmitAsync(cut, "someone@example.com");

        // Assert
        cut.Markup.ShouldContain(SentMessage);
        cut.FindAll("form").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("We couldn't send your request");
    }

    [Theory(DisplayName = "Should show exactly the same message for any email")]
    [InlineData("known@bowlneba.com")]
    [InlineData("nobody@example.com")]
    public async Task Submit_ShouldShowIdenticalMessage_ForAnyEmail(string email)
    {
        // Arrange
        using var response = new StubApiResponse<object> { IsSuccessStatusCode = true };
        SetupApi(response);
        var cut = _ctx.Render<ForgotPasswordPage>();

        // Act
        await SubmitAsync(cut, email);

        // Assert
        cut.Find(".neba-card [role=status], .neba-card [role=alert], .neba-card .neba-alert").TextContent.ShouldContain(SentMessage);
        cut.Markup.ShouldNotContain(email);
    }

    [Fact(DisplayName = "Should send the trimmed email to the API")]
    public async Task Submit_ShouldSendTrimmedEmail()
    {
        // Arrange
        ForgotPasswordRequest? sent = null;
        using var response = new StubApiResponse<object> { IsSuccessStatusCode = true };
        SetupApi(response, request => sent = request);
        var cut = _ctx.Render<ForgotPasswordPage>();

        // Act
        await SubmitAsync(cut, "  someone@example.com  ");

        // Assert
        sent.ShouldNotBeNull();
        sent.Email.ShouldBe("someone@example.com");
    }

    [Fact(DisplayName = "Should show a generic error and keep the form when the request fails")]
    public async Task Submit_ShouldShowGenericError_WhenApiFails()
    {
        // Arrange
        using var response = new StubApiResponse<object> { IsSuccessStatusCode = false, StatusCode = System.Net.HttpStatusCode.TooManyRequests };
        SetupApi(response);
        var cut = _ctx.Render<ForgotPasswordPage>();

        // Act
        await SubmitAsync(cut, "someone@example.com");

        // Assert
        cut.Markup.ShouldContain("We couldn't send your request. Please try again in a few minutes.");
        cut.Markup.ShouldNotContain(SentMessage);
        cut.Find("form").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should not call the API and should show a message when the email is invalid")]
    public async Task Submit_ShouldNotCallApi_WhenEmailInvalid()
    {
        // Arrange
        var cut = _ctx.Render<ForgotPasswordPage>();

        // Act
        await SubmitAsync(cut, "not-an-email");

        // Assert
        cut.Markup.ShouldContain("Enter a valid email address.");
        _mockApi.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Should not call the API and should ask for an email when the field is empty")]
    public async Task Submit_ShouldNotCallApi_WhenEmailEmpty()
    {
        // Arrange
        var cut = _ctx.Render<ForgotPasswordPage>();

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Email is required.");
        _mockApi.VerifyNoOtherCalls();
    }
}