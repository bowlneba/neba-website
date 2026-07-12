using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.FabCreateButton")]
public sealed class FabCreateButtonTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render an anchor linking to Href with Label as the accessible name and tooltip")]
    public void Render_ShouldLinkToHref_WithLabelAsAccessibleNameAndTooltip()
    {
        // Arrange & Act
        var cut = _ctx.Render<FabCreateButton>(parameters => parameters
            .Add(p => p.Href, "/news/new")
            .Add(p => p.Label, "Create Article"));

        // Assert
        var anchor = cut.Find("a.neba-fab");
        anchor.GetAttribute("href").ShouldBe("/news/new");
        anchor.GetAttribute("aria-label").ShouldBe("Create Article");
        anchor.GetAttribute("title").ShouldBe("Create Article");
    }
}
