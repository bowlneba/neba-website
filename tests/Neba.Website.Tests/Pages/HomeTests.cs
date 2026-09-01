using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Pages;

namespace Neba.Website.Tests.Pages;

[UnitTest]
[Component("Website.Pages.Home")]
public sealed class HomeTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should not skip a heading level between h1 and the first h2")]
    public void Render_ShouldNotSkipHeadingLevel_BetweenH1AndFirstH2()
    {
        // Act
        var cut = _ctx.Render<Home>();

        // Assert — the quick-link cards are h3s, so an h2 must appear before any of them.
        var headingLevels = cut.FindAll("h1, h2, h3, h4, h5, h6")
            .Select(h => int.Parse(h.TagName[1..], System.Globalization.CultureInfo.InvariantCulture))
            .ToList();

        headingLevels[0].ShouldBe(1);
        headingLevels[1].ShouldBe(2);
    }
}