using Bunit;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.SeriesFinaleCard")]
public sealed class SeriesFinaleCardTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<SeriesFinaleCard> Render(SeriesFinaleItemViewModel finale, bool isPast = false)
        => _ctx.Render<SeriesFinaleCard>(p => p
            .Add(x => x.Finale, finale)
            .Add(x => x.IsPast, isPast));

    [Fact(DisplayName = "Should title the card '{Series name} Finale' and link it to the Series")]
    public void Render_ShouldShowTitleLinkingToSeries()
    {
        // Arrange
        var finale = SeriesFinaleItemViewModelFactory.Create();

        // Act
        var cut = Render(finale);

        // Assert
        var link = cut.Find("h3 a");
        link.TextContent.ShouldBe("Vincent Hall Memorial Series Finale");
        link.GetAttribute("href").ShouldBe("/series/" + SeriesFinaleItemViewModelFactory.ValidSeriesId);
    }

    [Fact(DisplayName = "Should show the Series logo when it has one")]
    public void Render_ShouldShowSeriesLogo()
    {
        // Arrange
        var finale = SeriesFinaleItemViewModelFactory.Create(logoUrl: new Uri("https://storage.example.com/series/logo.png"));

        // Act
        var cut = Render(finale);

        // Assert
        cut.Find("img").GetAttribute("src").ShouldBe("https://storage.example.com/series/logo.png");
        cut.Find(".series-logo-tile").ClassList.ShouldContain("series-logo-tile--card");
    }

    [Fact(DisplayName = "Should show the Series initials, never a tournament logo, when it has no logo")]
    public void Render_ShouldShowInitials_WhenNoLogo()
    {
        // Arrange
        var finale = SeriesFinaleItemViewModelFactory.Create();

        // Act
        var cut = Render(finale);

        // Assert
        cut.FindAll("img").ShouldBeEmpty();
        cut.Find(".series-logo-tile__initials").TextContent.ShouldBe("VH");
    }

    [Fact(DisplayName = "Should show the date and time and a Finale chip in the eyebrow")]
    public void Render_ShouldShowDateTimeAndChip()
    {
        // Arrange
        var finale = SeriesFinaleItemViewModelFactory.Create();

        // Act
        var cut = Render(finale);

        // Assert
        var eyebrow = cut.Find(".series-finale-card__eyebrow");
        eyebrow.QuerySelector("time")!.TextContent.ShouldBe("Sat 9/27 at 2:00 PM");
        eyebrow.QuerySelector(".kind-chip")!.TextContent.Trim().ShouldBe("Finale");
        cut.Find(".series-finale-card").GetAttribute("aria-label")
            .ShouldBe("Vincent Hall Memorial Series Finale, Sat 9/27 at 2:00 PM");
    }

    [Fact(DisplayName = "Should say the host center is to be announced when there is none")]
    public void Render_ShouldShowTba_WhenNoHost()
    {
        // Arrange
        var finale = SeriesFinaleItemViewModelFactory.Create();

        // Act
        var cut = Render(finale);

        // Assert
        cut.Find(".series-finale-card__host").TextContent.Trim().ShouldContain("Host center to be announced");
    }

    [Fact(DisplayName = "Should show the host when there is one")]
    public void Render_ShouldShowHost()
    {
        // Arrange
        var finale = SeriesFinaleItemViewModelFactory.Create(host: "Callahan's Bowl-O-Rama, Seekonk, MA");

        // Act
        var cut = Render(finale);

        // Assert
        cut.Find(".series-finale-card__host").TextContent.ShouldContain("Callahan's Bowl-O-Rama, Seekonk, MA");
    }

    [Theory(DisplayName = "Should show the finalist count in singular and plural")]
    [InlineData(1, "1 finalist")]
    [InlineData(4, "4 finalists")]
    public void Render_ShouldShowFinalistCount(int count, string expected)
    {
        // Arrange
        var finale = SeriesFinaleItemViewModelFactory.Create(finalistCount: count);

        // Act
        var cut = Render(finale);

        // Assert
        cut.Find(".series-finale-card__chip").TextContent.ShouldBe(expected);
    }

    [Fact(DisplayName = "Should have no price, registration or edit-logo button, only a View Series link")]
    public void Render_ShouldHaveOnlyViewSeriesAction()
    {
        // Arrange
        var finale = SeriesFinaleItemViewModelFactory.Create();

        // Act
        var cut = Render(finale);

        // Assert
        cut.FindAll("button").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("Register");
        cut.Find(".series-finale-card__actions a").TextContent.ShouldContain("View Series");
        cut.Find(".series-finale-card__actions a").GetAttribute("href")
            .ShouldBe("/series/" + SeriesFinaleItemViewModelFactory.ValidSeriesId);
    }

    [Theory(DisplayName = "Should use the past styling only when the Finale is past")]
    [InlineData(true)]
    [InlineData(false)]
    public void Render_ShouldApplyPastStyling(bool isPast)
    {
        // Arrange
        var finale = SeriesFinaleItemViewModelFactory.Create();

        // Act
        var cut = Render(finale, isPast);

        // Assert
        cut.Find(".series-finale-card").ClassList.Contains("series-finale-card--past").ShouldBe(isPast);
    }
}
