using Bunit;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments;

namespace Neba.Website.Tests.Tournaments;

[UnitTest]
[Component("Website.Tournaments.TournamentSeriesTag")]
public sealed class TournamentSeriesTagTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should link to the Series page")]
    public void Render_ShouldLinkToSeries()
    {
        // Arrange
        var series = TournamentSeriesViewModelFactory.Create();

        // Act
        var cut = _ctx.Render<TournamentSeriesTag>(p => p.Add(x => x.Series, series));

        // Assert
        var link = cut.Find("a.tournament-series-tag");
        link.GetAttribute("href").ShouldBe("/series/" + TournamentSeriesViewModelFactory.ValidSeriesId);
        link.GetAttribute("title").ShouldBe("View the Series");
        link.TextContent.ShouldContain(TournamentSeriesViewModelFactory.ValidName);
    }

    [Theory(DisplayName = "Should show the round label with the matching kind class")]
    [InlineData("Qualifying", "Qualifying Round", "kind-qualifying")]
    [InlineData("Survivor", "Survivor Round", "kind-survivor")]
    [InlineData("Placement", "Placement Round", "kind-placement")]
    public void Render_ShouldShowRoundLabel(string kind, string label, string cssClass)
    {
        // Arrange
        var series = TournamentSeriesViewModelFactory.Create(kind: kind);

        // Act
        var cut = _ctx.Render<TournamentSeriesTag>(p => p.Add(x => x.Series, series));

        // Assert
        var chip = cut.Find(".kind-chip");
        chip.TextContent.Trim().ShouldBe(label);
        chip.ClassList.ShouldContain(cssClass);
    }

    [Fact(DisplayName = "Should show the logo with alt text when the Series has one")]
    public void Render_ShouldShowLogo()
    {
        // Arrange
        var series = TournamentSeriesViewModelFactory.Create(logoUrl: new Uri("https://storage.example.com/series/logo.png"));

        // Act
        var cut = _ctx.Render<TournamentSeriesTag>(p => p.Add(x => x.Series, series));

        // Assert
        cut.Find("img").GetAttribute("alt").ShouldBe(TournamentSeriesViewModelFactory.ValidName + " logo");
    }

    [Fact(DisplayName = "Should show initials when the Series has no logo")]
    public void Render_ShouldShowInitials_WhenNoLogo()
    {
        // Arrange
        var series = TournamentSeriesViewModelFactory.Create();

        // Act
        var cut = _ctx.Render<TournamentSeriesTag>(p => p.Add(x => x.Series, series));

        // Assert
        cut.FindAll("img").ShouldBeEmpty();
        cut.Find(".series-logo-tile__initials").TextContent.ShouldBe("VH");
        cut.Find(".series-logo-tile").ClassList.ShouldContain("series-logo-tile--tag");
    }
}
