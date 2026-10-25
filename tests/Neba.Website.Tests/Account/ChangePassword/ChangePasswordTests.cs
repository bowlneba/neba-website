using System.Security.Claims;

using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Security.ChangePassword;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;
using Neba.Website.Server.Account.Session;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Components;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

using ChangePasswordPage = Neba.Website.Server.Account.ChangePassword.ChangePassword;

namespace Neba.Website.Tests.Account.ChangePassword;

[UnitTest]
[Component("Website.Account.ChangePassword")]
public sealed class ChangePasswordTests : IDisposable
{
    private const string CurrentPassword = "OldPassword1";
    private const string NewPassword = "NewPassword1";

    private readonly BunitContext _ctx;
    private readonly Mock<ISecurityApi> _mockApi;
    private readonly Mock<ISessionCookieSync> _mockCookieSync;
    private readonly SessionTokenCache _tokenCache = new();

    public ChangePasswordTests()
    {
        _mockApi = new Mock<ISecurityApi>(MockBehavior.Strict);
        _mockCookieSync = new Mock<ISessionCookieSync>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.AddAuthorization()
            .SetAuthorized("dana")
            .SetClaims(new Claim(ClaimTypes.Email, "dana@bowlneba.com"));
        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(_mockCookieSync.Object);
        _ctx.Services.AddSingleton(_tokenCache);
        _ctx.Services.AddSingleton(sp => new ApiExecutor(mockStopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose() => _ctx.Dispose();

    private static async Task FillAsync(
        IRenderedComponent<ChangePasswordPage> cut,
        string current = CurrentPassword,
        string newPassword = NewPassword,
        string? confirm = null)
    {
        await cut.InvokeAsync(() => cut.Find("#current-password").Input(current));
        await cut.InvokeAsync(() => cut.FindAll("input[type=password]")[1].Input(newPassword));
        await cut.InvokeAsync(() => cut.FindAll("input[type=password]")[2].Input(confirm ?? newPassword));
    }

    private static async Task<StubApiResponse<ChangePasswordResponse>> RejectedAsync(System.Net.HttpStatusCode status, string reason)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/security/password/change");
        using var message = new HttpResponseMessage(status)
        {
            Content = new StringContent(
                "{\"status\":" + (int)status + ",\"errors\":[{\"name\":\"generalErrors\",\"reason\":\"" + reason + "\"}]}")
        };
        var exception = await ApiException.Create(request, HttpMethod.Post, message, new RefitSettings());

        return new StubApiResponse<ChangePasswordResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = status,
            Error = exception
        };
    }

    [Fact(DisplayName = "Should show the signed-in email in the subtitle")]
    public void Render_ShouldShowEmail()
    {
        // Arrange & Act
        var cut = _ctx.Render<ChangePasswordPage>();

        // Assert
        cut.Markup.ShouldContain("Choose a new password for dana@bowlneba.com.");
    }

    [Fact(DisplayName = "Should disable submit until every field is filled and the new password matches its confirmation")]
    public async Task Render_ShouldDisableSubmit_UntilFormIsValid()
    {
        // Arrange
        var cut = _ctx.Render<ChangePasswordPage>();
        cut.Find("button[type=submit]").HasAttribute("disabled").ShouldBeTrue();

        // Act & Assert
        await FillAsync(cut, confirm: "Different1");
        cut.Find("button[type=submit]").HasAttribute("disabled").ShouldBeTrue();

        await FillAsync(cut);
        cut.Find("button[type=submit]").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact(DisplayName = "Should disable submit and explain when the new password equals the current one")]
    public async Task Render_ShouldDisableSubmit_WhenNewPasswordEqualsCurrent()
    {
        // Arrange
        var cut = _ctx.Render<ChangePasswordPage>();

        // Act
        await FillAsync(cut, current: "SamePassword1", newPassword: "SamePassword1");

        // Assert
        cut.Find("button[type=submit]").HasAttribute("disabled").ShouldBeTrue();
        cut.Markup.ShouldContain("New password must be different from the current password.");
    }

    [Fact(DisplayName = "Should switch the circuit to the new tokens, update the cookie, and show success when the change succeeds")]
    public async Task Submit_ShouldSetTokensAndPersist_WhenSucceeds()
    {
        // Arrange
        var apiResponse = ChangePasswordResponseFactory.Create();
        using var response = new StubApiResponse<ChangePasswordResponse> { IsSuccessStatusCode = true, Content = apiResponse };
        _mockApi
            .Setup(api => api.ChangePasswordAsync(
                It.Is<ChangePasswordRequest>(r => r.CurrentPassword == CurrentPassword && r.NewPassword == NewPassword),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        _mockCookieSync
            .Setup(sync => sync.PersistAsync(
                It.Is<SessionTokens>(t =>
                    t.AccessToken == apiResponse.AccessToken
                    && t.RefreshToken == apiResponse.RefreshToken
                    && t.UserId == apiResponse.UserId
                    && t.Email == apiResponse.Email),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var cut = _ctx.Render<ChangePasswordPage>();
        await FillAsync(cut);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        _tokenCache.AccessToken.ShouldBe(apiResponse.AccessToken);
        _tokenCache.RefreshToken.ShouldBe(apiResponse.RefreshToken);
        cut.Markup.ShouldContain("Your password was changed.");
        cut.Find("#current-password").GetAttribute("value").ShouldBeNullOrEmpty();
        _mockCookieSync.VerifyAll();
    }

    [Fact(DisplayName = "Should warn and link to log in again when the session cookie cannot be updated")]
    public async Task Submit_ShouldShowWarning_WhenPersistFails()
    {
        // Arrange
        using var response = new StubApiResponse<ChangePasswordResponse>
        {
            IsSuccessStatusCode = true,
            Content = ChangePasswordResponseFactory.Create()
        };
        _mockApi
            .Setup(api => api.ChangePasswordAsync(It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        _mockCookieSync
            .Setup(sync => sync.PersistAsync(It.IsAny<SessionTokens>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var cut = _ctx.Render<ChangePasswordPage>();
        await FillAsync(cut);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("we couldn't refresh your session");
        cut.Find("a[href='/account/logout']").TextContent.ShouldBe("Log in again");
        cut.Markup.ShouldNotContain("Other devices will be signed out shortly.");
    }

    [Theory(DisplayName = "Should show the API message and leave the session alone when the change is rejected")]
    [InlineData(System.Net.HttpStatusCode.UnprocessableEntity, "Current password is incorrect.")]
    [InlineData(System.Net.HttpStatusCode.Conflict, "Too many incorrect attempts. Try again in a few minutes.")]
    public async Task Submit_ShouldShowApiMessage_WhenRejected(System.Net.HttpStatusCode status, string reason)
    {
        // Arrange
        using var response = await RejectedAsync(status, reason);
        _mockApi
            .Setup(api => api.ChangePasswordAsync(It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var cut = _ctx.Render<ChangePasswordPage>();
        await FillAsync(cut);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain(reason);
        _tokenCache.IsSeeded.ShouldBeFalse();
        _mockCookieSync.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Should not call the API when submitted with an invalid form")]
    public async Task Submit_ShouldNotCallApi_WhenFormInvalid()
    {
        // Arrange
        var cut = _ctx.Render<ChangePasswordPage>();

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        _mockApi.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Should turn the dirty guard on after typing and off after a successful change")]
    public async Task Render_ShouldTrackDirtyState()
    {
        // Arrange
        using var response = new StubApiResponse<ChangePasswordResponse>
        {
            IsSuccessStatusCode = true,
            Content = ChangePasswordResponseFactory.Create()
        };
        _mockApi
            .Setup(api => api.ChangePasswordAsync(It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        _mockCookieSync
            .Setup(sync => sync.PersistAsync(It.IsAny<SessionTokens>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var cut = _ctx.Render<ChangePasswordPage>();
        cut.FindComponent<DirtyFormGuard>().Instance.IsDirty.ShouldBeFalse();

        // Act & Assert
        await FillAsync(cut);
        cut.FindComponent<DirtyFormGuard>().Instance.IsDirty.ShouldBeTrue();

        await cut.Find("form").SubmitAsync();
        cut.FindComponent<DirtyFormGuard>().Instance.IsDirty.ShouldBeFalse();
    }
}
