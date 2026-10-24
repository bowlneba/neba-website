using Bunit;
using Bunit.TestDoubles;

using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.HallOfFame;

namespace Neba.Website.Tests.HallOfFame;

[UnitTest]
[Component("Website.HallOfFame.HallOfFameAvatar")]
public sealed class HallOfFameAvatarTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly BunitAuthorizationContext _authContext;

    public HallOfFameAvatarTests()
    {
        _authContext = _ctx.AddAuthorization();
    }

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<HallOfFameAvatar> Render(Action<ComponentParameterCollectionBuilder<HallOfFameAvatar>>? configure = null)
        => _ctx.Render<HallOfFameAvatar>(p =>
        {
            p.Add(x => x.BowlerName, "Jane Smith");
            configure?.Invoke(p);
        });

    [Fact(DisplayName = "Should show initials and no image when there is no photo")]
    public void Render_ShouldShowInitials_WhenNoPhoto()
    {
        // Arrange
        // Act
        var cut = Render();

        // Assert
        cut.Find(".inductee-photo").TextContent.Trim().ShouldBe("JS");
        cut.FindAll("img").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should position the photo from the saved focal point and zoom")]
    public void Render_ShouldPositionPhoto_FromFocalPointAndZoom()
    {
        // Arrange
        // Act
        var cut = Render(p => p
            .Add(x => x.PhotoUri, new Uri("https://files.example.test/jane.jpg"))
            .Add(x => x.FocalX, 31)
            .Add(x => x.FocalY, 36)
            .Add(x => x.Zoom, 150));

        // Assert
        var image = cut.Find("img");
        image.GetAttribute("src").ShouldBe("https://files.example.test/jane.jpg");
        image.GetAttribute("style").ShouldBe("object-position:31% 36%;transform-origin:31% 36%;transform:scale(1.5)");
        cut.Find(".inductee-photo").TextContent.Trim().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should center the photo with no zoom when no framing is saved")]
    public void Render_ShouldCenterPhoto_WhenNoFraming()
    {
        // Arrange
        // Act
        var cut = Render(p => p.Add(x => x.PhotoUri, new Uri("https://files.example.test/jane.jpg")));

        // Assert
        cut.Find("img").GetAttribute("style")
            .ShouldBe("object-position:50% 50%;transform-origin:50% 50%;transform:scale(1)");
    }

    [Fact(DisplayName = "Should not render the edit button when OnEdit is not set")]
    public void Render_ShouldNotRenderEditButton_WhenOnEditNotSet()
    {
        // Arrange
        _authContext.SetAuthorized("editor");
        _authContext.SetPolicies(Permissions.EditHallOfFamePhoto.PolicyName);

        // Act
        var cut = Render();

        // Assert
        cut.FindAll("button").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should hide the edit button from a user without the permission")]
    public void Render_ShouldHideEditButton_WithoutPermission()
    {
        // Arrange
        _authContext.SetAuthorized("member");

        // Act
        var cut = Render(p => p.Add(x => x.OnEdit, () => { }));

        // Assert
        cut.FindAll("button").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show the edit button to a user with the permission and raise OnEdit when it is clicked")]
    public void Render_ShouldShowEditButton_WithPermission()
    {
        // Arrange
        _authContext.SetAuthorized("editor");
        _authContext.SetPolicies(Permissions.EditHallOfFamePhoto.PolicyName);
        var clicked = false;
        var cut = Render(p => p.Add(x => x.OnEdit, () => clicked = true));

        // Act
        cut.Find("button.hof-photo-edit").Click();

        // Assert
        clicked.ShouldBeTrue();
    }

    [Theory(DisplayName = "GetInitials should return the expected initials")]
    [InlineData("Jane Smith", "JS")]
    [InlineData("Mary Jo Johnson", "MJ")]
    [InlineData("Cher", "C")]
    [InlineData("", "?")]
    public void GetInitials_ShouldReturnExpectedInitials(string name, string expected)
    {
        // Arrange
        // Act
        var initials = HallOfFameAvatar.GetInitials(name);

        // Assert
        initials.ShouldBe(expected);
    }
}
