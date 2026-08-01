using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;
using Neba.Website.Server.Help;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.HelpButton")]
public sealed class HelpButtonTests : IDisposable
{
    private readonly BunitContext _ctx;

    public HelpButtonTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddSingleton<HelpDocumentService>();
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should not render the modal backdrop before the button is clicked")]
    public void Render_ShouldNotRenderModalBackdrop_BeforeButtonClicked()
    {
        // Arrange & Act
        var cut = _ctx.Render<HelpButton>(p => p.Add(x => x.DocName, "create-sponsor"));

        // Assert
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render the rendered doc content in the modal when the button is clicked")]
    public void Click_ShouldRenderDocContent_WhenDocExists()
    {
        // Arrange
        var cut = _ctx.Render<HelpButton>(p => p.Add(x => x.DocName, "create-sponsor"));

        // Act
        cut.Find("button.neba-help-btn").Click();

        // Assert
        cut.Find(".neba-modal-backdrop").ShouldNotBeNull();
        cut.Markup.ShouldContain("Create a Sponsor");
    }

    [Fact(DisplayName = "Should show a fallback message when the doc doesn't exist")]
    public void Click_ShouldShowFallbackMessage_WhenDocDoesNotExist()
    {
        // Arrange
        var cut = _ctx.Render<HelpButton>(p => p.Add(x => x.DocName, "no-such-doc"));

        // Act
        cut.Find("button.neba-help-btn").Click();

        // Assert
        cut.Markup.ShouldContain("Help content isn't available for this page yet.");
    }

    [Fact(DisplayName = "Should apply the light styling class when Light is true")]
    public void Render_ShouldApplyLightClass_WhenLightIsTrue()
    {
        // Arrange & Act
        var cut = _ctx.Render<HelpButton>(p => p
            .Add(x => x.DocName, "create-sponsor")
            .Add(x => x.Light, true));

        // Assert
        cut.Find("button.neba-help-btn").ClassList.ShouldContain("neba-help-btn--light");
    }

    [Fact(DisplayName = "Should not apply the light styling class by default")]
    public void Render_ShouldNotApplyLightClass_ByDefault()
    {
        // Arrange & Act
        var cut = _ctx.Render<HelpButton>(p => p.Add(x => x.DocName, "create-sponsor"));

        // Assert
        cut.Find("button.neba-help-btn").ClassList.ShouldNotContain("neba-help-btn--light");
    }
}
