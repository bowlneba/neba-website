using System.Net;
using System.Security.Claims;

using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contracts.Cache;
using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Layout;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.Layout;

[UnitTest]
[Component("Website.Layout.AccountMenu")]
public sealed class AccountMenuTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly BunitAuthorizationContext _authContext;
    private readonly Mock<ICacheApi> _mockCacheApi;
    private readonly ToastService _toastService;

    public AccountMenuTests()
    {
        _ctx = new BunitContext();
        _authContext = _ctx.AddAuthorization();
        _ctx.Services.AddScoped<CircuitTokenCache>();

        _toastService = new ToastService();
        _ctx.Services.AddSingleton(_toastService);

        _ctx.Services.AddSingleton(new NebaApiConfiguration { BaseUrl = new Uri("https://api.bowlneba.com") });

        _mockCacheApi = new Mock<ICacheApi>(MockBehavior.Strict);
        _ctx.Services.AddSingleton(_mockCacheApi.Object);

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.SetupGet(m => m.HttpContext).Returns((HttpContext?)null);
        _ctx.Services.AddSingleton(httpContextAccessorMock.Object);

        SetEnvironment("Development");
    }

    // Non-Production by default so the existing query-string-token tests below reflect local dev,
    // where AccountConfiguration doesn't set the shared auth cookie's Domain (see AccountMenu's own
    // comment) and this fallback is the only way the dashboard link authenticates.
    private void SetEnvironment(string environmentName)
    {
        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>(MockBehavior.Strict);
        mockWebHostEnvironment.Setup(x => x.EnvironmentName).Returns(environmentName);
        _ctx.Services.AddSingleton(mockWebHostEnvironment.Object);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
    }

    [Fact(DisplayName = "Should render nothing when user is not authorized")]
    public void Render_ShouldRenderNothing_WhenUserIsNotAuthorized()
    {
        // Arrange
        _authContext.SetNotAuthorized();

        // Act
        var cut = _ctx.Render<AccountMenu>();

        // Assert
        cut.FindAll("div.account-menu").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render account trigger when user is authorized")]
    public void Render_ShouldRenderAccountTrigger_WhenUserIsAuthorized()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");

        // Act
        var cut = _ctx.Render<AccountMenu>();

        // Assert
        cut.Find("div.account-menu").ShouldNotBeNull();
        cut.Find("button.account-trigger").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should not use ARIA menu roles that promise keyboard menu behavior it doesn't implement")]
    public void Render_ShouldNotUseAriaMenuRoles()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies();

        // Act
        var cut = _ctx.Render<AccountMenu>();

        // Assert
        cut.FindAll("[role='menu']").ShouldBeEmpty();
        cut.FindAll("[role='menuitem']").ShouldBeEmpty();
        cut.Find("button.account-trigger").GetAttribute("aria-label").ShouldBe("Account menu");
    }

    [Fact(DisplayName = "Should display the user's email and a logout link when authorized")]
    public void Render_ShouldDisplayEmailAndLogoutLink_WhenUserIsAuthorized()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetClaims(new Claim(ClaimTypes.Email, "bowler@bowlneba.com"));
        _authContext.SetPolicies();

        // Act
        var cut = _ctx.Render<AccountMenu>();

        // Assert
        cut.Find("span.account-email").TextContent.ShouldBe("bowler@bowlneba.com");
        cut.Find("a.account-dropdown-link").GetAttribute("href").ShouldBe("/account/logout");
    }

    [Fact(DisplayName = "Should show the Create User link when the user holds the CreateUser policy")]
    public void Render_ShouldShowCreateUserLink_WhenUserHoldsPolicy()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.CreateUser.PolicyName);

        // Act
        var cut = _ctx.Render<AccountMenu>();

        // Assert
        cut.FindAll("a.account-dropdown-link")
            .ShouldContain(a => a.GetAttribute("href") == "/account/create-user" && a.TextContent == "Create User");
    }

    [Fact(DisplayName = "Should not show the Create User link when the user lacks the CreateUser policy")]
    public void Render_ShouldNotShowCreateUserLink_WhenUserLacksPolicy()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies();

        // Act
        var cut = _ctx.Render<AccountMenu>();

        // Assert
        cut.FindAll("a.account-dropdown-link").ShouldNotContain(a => a.GetAttribute("href") == "/account/create-user");
    }

    [Fact(DisplayName = "Should show the Background Jobs link with the access token in the query string when the user holds the permission")]
    public void Render_ShouldShowBackgroundJobsLink_WhenUserHoldsPolicyAndTokenIsAvailable()
    {
        // Arrange
        _ctx.Services.GetRequiredService<CircuitTokenCache>().AccessToken = "test-token";
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.ViewBackgroundJobsDashboard.PolicyName);

        // Act
        var cut = _ctx.Render<AccountMenu>();

        // Assert
        var link = cut.FindAll("a.account-dropdown-link")
            .Where(a => a.TextContent == "Background Jobs")
            .ShouldHaveSingleItem();
        link.GetAttribute("href").ShouldBe("https://api.bowlneba.com/background-jobs?access_token=test-token");
    }

    [Fact(DisplayName = "Should not show the Background Jobs link when the user lacks the permission")]
    public void Render_ShouldNotShowBackgroundJobsLink_WhenUserLacksPolicy()
    {
        // Arrange
        _ctx.Services.GetRequiredService<CircuitTokenCache>().AccessToken = "test-token";
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies();

        // Act
        var cut = _ctx.Render<AccountMenu>();

        // Assert
        cut.FindAll("a.account-dropdown-link").ShouldNotContain(a => a.TextContent == "Background Jobs");
    }

    [Fact(DisplayName = "Should not show the Background Jobs link when no access token is available")]
    public void Render_ShouldNotShowBackgroundJobsLink_WhenNoTokenIsAvailable()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.ViewBackgroundJobsDashboard.PolicyName);

        // Act
        var cut = _ctx.Render<AccountMenu>();

        // Assert
        cut.FindAll("a.account-dropdown-link").ShouldNotContain(a => a.TextContent == "Background Jobs");
    }

    [Fact(DisplayName = "Should show the Background Jobs link without an access token in production, even when no token is available")]
    public void Render_ShouldShowBackgroundJobsLinkWithoutToken_WhenEnvironmentIsProduction()
    {
        // Arrange - production relies on the shared auth cookie (AccountConfiguration sets its
        // Domain to the parent domain both apps share), not the query-string fallback, so the link
        // should still appear even with no access token cached anywhere.
        SetEnvironment("Production");
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.ViewBackgroundJobsDashboard.PolicyName);

        // Act
        var cut = _ctx.Render<AccountMenu>();

        // Assert
        var link = cut.FindAll("a.account-dropdown-link")
            .Where(a => a.TextContent == "Background Jobs")
            .ShouldHaveSingleItem();
        link.GetAttribute("href").ShouldBe("https://api.bowlneba.com/background-jobs");
    }

    [Fact(DisplayName = "Should not include the access token in the Background Jobs link in production, even when a token is available")]
    public void Render_ShouldNotIncludeAccessToken_WhenEnvironmentIsProductionAndTokenIsAvailable()
    {
        // Arrange - production relies on the shared auth cookie, not the query string, so a
        // cached token must never leak into the link even when one happens to be available.
        SetEnvironment("Production");
        _ctx.Services.GetRequiredService<CircuitTokenCache>().AccessToken = "test-token";
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.ViewBackgroundJobsDashboard.PolicyName);

        // Act
        var cut = _ctx.Render<AccountMenu>();

        // Assert
        var link = cut.FindAll("a.account-dropdown-link")
            .Where(a => a.TextContent == "Background Jobs")
            .ShouldHaveSingleItem();
        var href = link.GetAttribute("href");
        href.ShouldNotBeNull();
        href.ShouldNotContain("access_token");
        href.ShouldNotContain("test-token");
    }

    [Fact(DisplayName = "Should show the Clear Cache button when the user holds the ClearCache policy")]
    public void Render_ShouldShowClearCacheButton_WhenUserHoldsPolicy()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.ClearCache.PolicyName);

        // Act
        var cut = _ctx.Render<AccountMenu>();

        // Assert
        cut.FindAll("button.account-dropdown-link")
            .ShouldContain(b => b.TextContent == "Clear Cache");
    }

    [Fact(DisplayName = "Should not show the Clear Cache button when the user lacks the ClearCache policy")]
    public void Render_ShouldNotShowClearCacheButton_WhenUserLacksPolicy()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies();

        // Act
        var cut = _ctx.Render<AccountMenu>();

        // Assert
        cut.FindAll("button.account-dropdown-link").ShouldNotContain(b => b.TextContent == "Clear Cache");
    }

    [Fact(DisplayName = "Should show a success toast when clearing the cache succeeds")]
    public async Task ClearCache_ShouldShowSuccessToast_WhenApiCallSucceeds()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.ClearCache.PolicyName);

        using var apiResponse = new StubApiResponse<object> { IsSuccessStatusCode = true };
        _mockCacheApi
            .Setup(a => a.ClearCacheAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        var cut = _ctx.Render<AccountMenu>();

        // Act
        await cut.InvokeAsync(() => cut.Find("button.account-dropdown-link").Click());

        // Assert
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Success);
        _toastService.Current.Title.ShouldBe("Cache Cleared");
    }

    [Fact(DisplayName = "Should show a failure toast with the status code when clearing the cache returns a non-success status")]
    public async Task ClearCache_ShouldShowFailureToastWithStatusCode_WhenApiCallReturnsNonSuccessStatus()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.ClearCache.PolicyName);

        using var apiResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = false,
            StatusCode = HttpStatusCode.Forbidden
        };
        _mockCacheApi
            .Setup(a => a.ClearCacheAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        var cut = _ctx.Render<AccountMenu>();

        // Act
        await cut.InvokeAsync(() => cut.Find("button.account-dropdown-link").Click());

        // Assert
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Error);
        _toastService.Current.Title.ShouldBe("Cache Clear Failed");
        _toastService.Current.Message.ShouldContain("403");
    }

    [Fact(DisplayName = "Should show a failure toast with the exception message when clearing the cache throws")]
    public async Task ClearCache_ShouldShowFailureToastWithExceptionMessage_WhenApiCallThrows()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.ClearCache.PolicyName);

        _mockCacheApi
            .Setup(a => a.ClearCacheAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var cut = _ctx.Render<AccountMenu>();

        // Act
        await cut.InvokeAsync(() => cut.Find("button.account-dropdown-link").Click());

        // Assert
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Error);
        _toastService.Current.Title.ShouldBe("Cache Clear Failed");
        _toastService.Current.Message.ShouldBe("Connection refused");
    }
}