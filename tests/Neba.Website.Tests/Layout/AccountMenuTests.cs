using System.Security.Claims;

using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Layout;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Services;

namespace Neba.Website.Tests.Layout;

[UnitTest]
[Component("Website.Layout.AccountMenu")]
public sealed class AccountMenuTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly BunitAuthorizationContext _authContext;

    public AccountMenuTests()
    {
        _ctx = new BunitContext();
        _authContext = _ctx.AddAuthorization();
        _ctx.Services.AddScoped<CircuitTokenCache>();
        _ctx.Services.AddScoped<ToastService>();
        _ctx.Services.AddSingleton(new Mock<IHttpContextAccessor>(MockBehavior.Strict).Object);
    }

    public void Dispose() => _ctx.Dispose();

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
}