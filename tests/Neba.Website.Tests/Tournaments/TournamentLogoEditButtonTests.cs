using Bunit;
using Bunit.TestDoubles;

using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Tournaments;

namespace Neba.Website.Tests.Tournaments;

[UnitTest]
[Component("Website.Tournaments.TournamentLogoEditButton")]
public sealed class TournamentLogoEditButtonTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly BunitAuthorizationContext _authContext;

    public TournamentLogoEditButtonTests()
    {
        _authContext = _ctx.AddAuthorization();
    }

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<TournamentLogoEditButton> Render(bool showLabel = false, Action? onClick = null)
        => _ctx.Render<TournamentLogoEditButton>(p =>
        {
            p.Add(x => x.TournamentName, "Spring Open");
            p.Add(x => x.ShowLabel, showLabel);
            p.Add(x => x.OnClick, onClick ?? (() => { }));
        });

    [Fact(DisplayName = "Should render the button when the caller holds the EditTournamentLogo permission")]
    public void Render_ShouldShowButton_WhenPolicySatisfied()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.EditTournamentLogo.PolicyName);

        // Act
        var cut = Render();

        // Assert
        cut.FindAll("button.tournament-logo-edit").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should render nothing when the caller does not satisfy the policy")]
    public void Render_ShouldRenderNothing_WhenPolicyNotSatisfied()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");

        // Act
        var cut = Render();

        // Assert
        cut.FindAll("button").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render nothing for an anonymous caller")]
    public void Render_ShouldRenderNothing_WhenAnonymous()
    {
        // Arrange
        _authContext.SetNotAuthorized();

        // Act
        var cut = Render();

        // Assert
        cut.FindAll("button").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should name the tournament in the accessible label")]
    public void Render_ShouldIncludeTournamentNameInAriaLabel()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.EditTournamentLogo.PolicyName);

        // Act
        var cut = Render();

        // Assert
        cut.Find("button").GetAttribute("aria-label").ShouldBe("Edit logo for Spring Open");
    }

    [Theory(DisplayName = "Should show the text label only when ShowLabel is true")]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Render_ShouldShowTextLabel_OnlyWhenRequested(bool showLabel, bool expectLabel)
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.EditTournamentLogo.PolicyName);

        // Act
        var cut = Render(showLabel);

        // Assert
        cut.Find("button").TextContent.Contains("Edit logo", StringComparison.Ordinal).ShouldBe(expectLabel);
    }

    [Fact(DisplayName = "Should invoke OnClick when clicked")]
    public async Task Click_ShouldInvokeOnClick()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.EditTournamentLogo.PolicyName);
        var clicks = 0;
        var cut = Render(onClick: () => clicks++);

        // Act
        await cut.Find("button").ClickAsync();

        // Assert
        clicks.ShouldBe(1);
    }
}
