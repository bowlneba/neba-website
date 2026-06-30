using System.Security.Claims;

using Bunit;
using Bunit.TestDoubles;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Layout;

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

        // Act
        var cut = _ctx.Render<AccountMenu>();

        // Assert
        cut.Find("span.account-email").TextContent.ShouldBe("bowler@bowlneba.com");
        cut.Find("a.account-dropdown-link").GetAttribute("href").ShouldBe("/account/logout");
    }
}
