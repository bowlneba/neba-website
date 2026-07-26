using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.PasswordFields")]
public sealed class PasswordFieldsTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Theory(DisplayName = "Should toggle each requirement independently as the password meets it")]
    [InlineData("short", "At least 8 characters", false)]
    [InlineData("longenough", "At least 8 characters", true)]
    [InlineData("nouppercase1", "One uppercase letter", false)]
    [InlineData("HasUppercase1", "One uppercase letter", true)]
    public void PasswordInput_ShouldToggleRequirement_BasedOnContent(string password, string requirementText, bool expectMet)
    {
        // Arrange
        var cut = _ctx.Render<PasswordFields>(p => p.Add(x => x.Label, "New Password"));

        // Act
        cut.Find("input[type=password]").Input(password);

        // Assert
        var item = cut.FindAll("li").First(li => li.TextContent.Contains(requirementText, StringComparison.Ordinal));
        item.ClassList.Contains("password-requirement-met").ShouldBe(expectMet);
    }

    [Fact(DisplayName = "Should render the Confirm label using the Label parameter")]
    public void Render_ShouldUseLabelParameter_ForBothFieldLabels()
    {
        // Arrange & Act
        var cut = _ctx.Render<PasswordFields>(p => p.Add(x => x.Label, "Password"));

        // Assert
        cut.Markup.ShouldContain(">Password<");
        cut.Markup.ShouldContain(">Confirm Password<");
    }

    [Fact(DisplayName = "Should not show the mismatch message until Confirm Password has content")]
    public void ConfirmInput_ShouldStayHidden_UntilConfirmHasContent()
    {
        // Arrange
        var cut = _ctx.Render<PasswordFields>(p => p.Add(x => x.Label, "New Password"));

        // Act
        cut.FindAll("input[type=password]")[0].Input("SomePassword1");

        // Assert
        cut.Markup.ShouldNotContain("Passwords do not match.");
    }

    [Fact(DisplayName = "Should show the mismatch message once Confirm Password differs from Password")]
    public void ConfirmInput_ShouldShowMismatch_WhenValuesDiffer()
    {
        // Arrange
        var cut = _ctx.Render<PasswordFields>(p => p.Add(x => x.Label, "New Password"));

        // Act
        cut.FindAll("input[type=password]")[0].Input("SomePassword1");
        cut.FindAll("input[type=password]")[1].Input("Different1");

        // Assert
        cut.Markup.ShouldContain("Passwords do not match.");
    }

    [Theory(DisplayName = "Should raise the strength meter tier as score-increasing characteristics are added")]
    [InlineData("abcdefgh", "Weak")]
    [InlineData("abcdefg1", "Fair")]
    [InlineData("Abcdefg1", "Good")]
    [InlineData("Abcdefghijkl1", "Strong")]
    public void PasswordInput_ShouldReachExpectedStrengthTier(string password, string expectedTier)
    {
        // Arrange
        var cut = _ctx.Render<PasswordFields>(p => p.Add(x => x.Label, "New Password"));

        // Act
        cut.Find("input[type=password]").Input(password);

        // Assert
        cut.Find(".password-strength-label").TextContent.ShouldContain(expectedTier);
    }
}
