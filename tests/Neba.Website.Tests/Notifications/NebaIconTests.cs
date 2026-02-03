using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Notifications;

namespace Neba.Website.Tests.Notifications;

[UnitTest]
[Component("Website.Notifications.NebaIcon")]
public sealed class NebaIconTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render success icon SVG when severity is Success")]
    public void Render_ShouldRenderSuccessIcon_WhenSeverityIsSuccess()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaIcon>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Success));

        // Assert
        var svg = cut.FindAll("svg");
        svg.Count.ShouldBe(1);
        cut.Markup.ShouldContain("M8 15L3 10L4.41 8.59L8 12.17L15.59 4.58L17 6L8 15Z");
    }

    [Fact(DisplayName = "Should render error icon SVG when severity is Error")]
    public void Render_ShouldRenderErrorIcon_WhenSeverityIsError()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaIcon>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Error));

        // Assert
        var svg = cut.FindAll("svg");
        svg.Count.ShouldBe(1);
        cut.Markup.ShouldContain("M11 15H9V13H11V15ZM11 11H9V5H11V11Z");
    }

    [Fact(DisplayName = "Should render warning icon SVG when severity is Warning")]
    public void Render_ShouldRenderWarningIcon_WhenSeverityIsWarning()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaIcon>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Warning));

        // Assert
        var svg = cut.FindAll("svg");
        svg.Count.ShouldBe(1);
        cut.Markup.ShouldContain("M1 17H19L10 2L1 17ZM11 14H9V12H11V14ZM11 10H9V6H11V10Z");
    }

    [Fact(DisplayName = "Should render info icon SVG when severity is Info")]
    public void Render_ShouldRenderInfoIcon_WhenSeverityIsInfo()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaIcon>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Info));

        // Assert
        var svg = cut.FindAll("svg");
        svg.Count.ShouldBe(1);
        cut.Markup.ShouldContain("M11 15H9V9H11V15ZM11 7H9V5H11V7Z");
    }

    [Fact(DisplayName = "Should render normal icon SVG when severity is Normal")]
    public void Render_ShouldRenderNormalIcon_WhenSeverityIsNormal()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaIcon>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Normal));

        // Assert
        var svg = cut.FindAll("svg");
        svg.Count.ShouldBe(1);
        cut.Markup.ShouldContain("circle");
    }

    [Fact(DisplayName = "Should apply custom CSS class to SVG")]
    public void Render_ShouldApplyCssClass_WhenCssClassIsProvided()
    {
        // Arrange
        const string cssClass = "custom-icon-class";

        // Act
        var cut = _ctx.Render<NebaIcon>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Success)
            .Add(p => p.CssClass, cssClass));

        // Assert
        var svg = cut.Find("svg");
        var classes = svg.GetAttribute("class");
        classes.ShouldNotBeNull();
        classes.ShouldContain(cssClass);
    }

    [Fact(DisplayName = "Should have aria-hidden attribute on SVG")]
    public void Render_ShouldHaveAriaHidden_OnSvg()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaIcon>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Info));

        // Assert
        var svg = cut.Find("svg");
        svg.GetAttribute("aria-hidden").ShouldBe("true");
    }

    [Fact(DisplayName = "Should have correct width and height dimensions")]
    public void Render_ShouldHaveCorrectDimensions_OnSvg()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaIcon>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Info));

        // Assert
        var svg = cut.Find("svg");
        svg.GetAttribute("width").ShouldBe("20");
        svg.GetAttribute("height").ShouldBe("20");
    }

    [Fact(DisplayName = "Should use currentColor for SVG fill")]
    public void Render_ShouldUseCurrentColor_ForSvgFill()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaIcon>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Error));

        // Assert
        var path = cut.Find("path");
        path.GetAttribute("fill").ShouldBe("currentColor");
    }

    [Theory(DisplayName = "Should render correct icon for all severity levels")]
    [InlineData(NotifySeverity.Success, TestDisplayName = "Success")]
    [InlineData(NotifySeverity.Error, TestDisplayName = "Error")]
    [InlineData(NotifySeverity.Warning, TestDisplayName = "Warning")]
    [InlineData(NotifySeverity.Info, TestDisplayName = "Info")]
    [InlineData(NotifySeverity.Normal, TestDisplayName = "Normal")]
    public void Render_ShouldRenderIcon_ForAllSeverities(NotifySeverity severity)
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaIcon>(parameters => parameters
            .Add(p => p.Severity, severity));

        // Assert
        cut.FindAll("svg").Count.ShouldBe(1);
    }
}