using Bunit;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments.Detail;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.TournamentSeriesCallout")]
public sealed class TournamentSeriesCalloutTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should show the Series name, round label and a View Series link")]
    public void Render_ShouldShowNameRoundAndLink()
    {
        // Arrange
        var series = TournamentSeriesViewModelFactory.Create(kind: "Qualifying");

        // Act
        var cut = _ctx.Render<TournamentSeriesCallout>(p => p.Add(x => x.Series, series));

        // Assert
        cut.Find("h2").TextContent.ShouldBe(TournamentSeriesViewModelFactory.ValidName);
        cut.Find(".kind-chip").TextContent.Trim().ShouldBe("Qualifying Round");
        var link = cut.Find("a.neba-btn");
        link.GetAttribute("href").ShouldBe("/series/" + TournamentSeriesViewModelFactory.ValidSeriesId);
        link.TextContent.ShouldContain("View Series");
    }

    [Fact(DisplayName = "Should say the top finishers enter the field for singles Qualifying")]
    public void Render_ShouldShowSinglesRule()
    {
        // Arrange
        var series = TournamentSeriesViewModelFactory.Create(kind: "Qualifying", advancingCount: 8);

        // Act
        var cut = _ctx.Render<TournamentSeriesCallout>(p => p.Add(x => x.Series, series));

        // Assert
        cut.Find(".tournament-series-callout__rule").TextContent.ShouldBe("Top 8 finishers enter the Series field.");
        cut.FindAll(".tournament-series-callout__rule-detail").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show the bowler count on the second line for team Qualifying")]
    public void Render_ShouldShowTeamRule()
    {
        // Arrange
        var series = TournamentSeriesViewModelFactory.Create(kind: "Qualifying", advancingCount: 2);

        // Act
        var cut = _ctx.Render<TournamentSeriesCallout>(p => p
            .Add(x => x.Series, series)
            .Add(x => x.TeamSize, 3));

        // Assert
        cut.Find(".tournament-series-callout__rule").TextContent.ShouldBe("Top 2 teams enter the Series field.");
        cut.Find(".tournament-series-callout__rule-detail").TextContent.ShouldBe("(6 bowlers)");
    }

    [Fact(DisplayName = "Should show the squad line for Survivor")]
    public void Render_ShouldShowSurvivorSquad()
    {
        // Arrange
        var series = TournamentSeriesViewModelFactory.Create(
            kind: "Survivor", advancingCount: 40, squadDate: new DateOnly(2025, 7, 12), squadStartTime: new TimeOnly(9, 0));

        // Act
        var cut = _ctx.Render<TournamentSeriesCallout>(p => p.Add(x => x.Series, series));

        // Assert
        cut.Find(".tournament-series-callout__rule").TextContent.ShouldBe("40 bowlers advance.");
        cut.Find(".tournament-series-callout__rule-detail").TextContent.ShouldBe("Series squad: 9:00 AM on 7/12.");
    }

    [Fact(DisplayName = "Should name the Finale date and time on a Placement Round with a Finale")]
    public void Render_ShouldShowFinale()
    {
        // Arrange
        var series = TournamentSeriesViewModelFactory.Create(
            kind: "Placement", advancingCount: null, finaleCount: 4,
            finaleDate: new DateOnly(2025, 9, 27), finaleStartTime: new TimeOnly(14, 0));

        // Act
        var cut = _ctx.Render<TournamentSeriesCallout>(p => p.Add(x => x.Series, series));

        // Assert
        cut.Find(".tournament-series-callout__rule").TextContent.ShouldBe("Final standings. The top 4 go to the Finale.");
        cut.Find(".tournament-series-callout__rule-detail").TextContent.ShouldBe("Finale: Sat 9/27 at 2:00 PM.");
    }

    [Fact(DisplayName = "Should say there is no Finale on a Placement Round without one")]
    public void Render_ShouldShowNoFinale()
    {
        // Arrange
        var series = TournamentSeriesViewModelFactory.Create(kind: "Placement", advancingCount: null);

        // Act
        var cut = _ctx.Render<TournamentSeriesCallout>(p => p.Add(x => x.Series, series));

        // Assert
        cut.Find(".tournament-series-callout__rule").TextContent.ShouldBe("Final standings (no Finale).");
        cut.FindAll(".tournament-series-callout__rule-detail").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show initials when the Series has no logo")]
    public void Render_ShouldShowInitials_WhenNoLogo()
    {
        // Arrange
        var series = TournamentSeriesViewModelFactory.Create();

        // Act
        var cut = _ctx.Render<TournamentSeriesCallout>(p => p.Add(x => x.Series, series));

        // Assert
        cut.FindAll("img").ShouldBeEmpty();
        cut.Find(".series-logo-tile__initials").TextContent.ShouldBe("VH");
    }
}
