using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Series;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesLogoTileTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should show the logo with alt text when a logo URL is set")]
    public void Render_ShouldShowImage_WhenLogoSet()
    {
        // Arrange & Act
        var cut = _ctx.Render<SeriesLogoTile>(p => p
            .Add(x => x.LogoUrl, new Uri("https://storage.example.com/series/logo.png"))
            .Add(x => x.Name, "2025 Vincent Hall Memorial Series"));

        // Assert
        var image = cut.Find("img");
        image.GetAttribute("alt").ShouldBe("2025 Vincent Hall Memorial Series logo");
        image.GetAttribute("src").ShouldBe("https://storage.example.com/series/logo.png");
        cut.FindAll(".series-logo-tile__initials").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show the initials when there is no logo")]
    public void Render_ShouldShowInitials_WhenNoLogo()
    {
        // Arrange & Act
        var cut = _ctx.Render<SeriesLogoTile>(p => p.Add(x => x.Name, "2025 Vincent Hall Memorial Series"));

        // Assert
        cut.Find(".series-logo-tile__initials").TextContent.ShouldBe("VH");
        cut.FindAll("img").ShouldBeEmpty();
    }

    [Theory(DisplayName = "Should apply the size class")]
    [InlineData(SeriesLogoTile.Large, "series-logo-tile--large")]
    [InlineData(SeriesLogoTile.Small, "series-logo-tile--small")]
    public void Render_ShouldApplySizeClass(string size, string expected)
    {
        // Arrange & Act
        var cut = _ctx.Render<SeriesLogoTile>(p => p
            .Add(x => x.Name, "Spring Series")
            .Add(x => x.Size, size));

        // Assert
        cut.Find(".series-logo-tile").ClassList.ShouldContain(expected);
    }
}