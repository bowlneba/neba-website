using Bunit;
using Bunit.TestDoubles;

using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.HallOfFame;

namespace Neba.Website.Tests.HallOfFame;

[UnitTest]
[Component("Website.HallOfFame.HallOfFamePhotoEditButton")]
public sealed class HallOfFamePhotoEditButtonTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly BunitAuthorizationContext _authContext;

    public HallOfFamePhotoEditButtonTests()
    {
        _authContext = _ctx.AddAuthorization();
    }

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<HallOfFamePhotoEditButton> Render(Action? onClick = null)
        => _ctx.Render<HallOfFamePhotoEditButton>(p =>
        {
            p.Add(x => x.BowlerName, "Jane Smith");
            p.Add(x => x.OnClick, onClick ?? (() => { }));
        });

    [Fact(DisplayName = "Should render the button when the caller holds the EditHallOfFamePhoto permission")]
    public void Render_ShouldShowButton_WhenPolicySatisfied()
    {
        // Arrange
        _authContext.SetAuthorized("editor");
        _authContext.SetPolicies(Permissions.EditHallOfFamePhoto.PolicyName);

        // Act
        var cut = Render();

        // Assert
        cut.FindAll("button.hof-photo-edit").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should render nothing when the caller does not satisfy the policy")]
    public void Render_ShouldRenderNothing_WhenPolicyNotSatisfied()
    {
        // Arrange
        _authContext.SetAuthorized("member");

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

    [Fact(DisplayName = "Should name the inductee in the accessible label")]
    public void Render_ShouldIncludeBowlerNameInAriaLabel()
    {
        // Arrange
        _authContext.SetAuthorized("editor");
        _authContext.SetPolicies(Permissions.EditHallOfFamePhoto.PolicyName);

        // Act
        var cut = Render();

        // Assert
        cut.Find("button").GetAttribute("aria-label").ShouldBe("Edit photo for Jane Smith");
    }

    [Fact(DisplayName = "Should raise OnClick when the button is clicked")]
    public void Click_ShouldRaiseOnClick()
    {
        // Arrange
        _authContext.SetAuthorized("editor");
        _authContext.SetPolicies(Permissions.EditHallOfFamePhoto.PolicyName);
        var clicked = false;
        var cut = Render(() => clicked = true);

        // Act
        cut.Find("button").Click();

        // Assert
        clicked.ShouldBeTrue();
    }
}