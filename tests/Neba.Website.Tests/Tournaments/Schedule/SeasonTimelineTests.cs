using Bunit;

using Microsoft.AspNetCore.Components.Web;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.SeasonTimeline")]
public sealed class SeasonTimelineTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render one timeline dot per tournament")]
    public void Render_ShouldShowOneDotPerTournament_WhenTournamentsProvided()
    {
        var tournaments = SeasonTournamentViewModelFactory.Bogus(3, seed: 2101).ToList();

        var cut = _ctx.Render<SeasonTimeline>(parameters => parameters
            .Add(p => p.Season, DateTime.Today.Year.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Add(p => p.Tournaments, tournaments));

        cut.FindAll(".season-timeline__dot").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Should show tooltip on dot hover and hide on mouse out")]
    public void Tooltip_ShouldShowAndHide_WhenDotHoveredThenMouseOut()
    {
        var tournament = SeasonTournamentViewModelFactory.Create();

        var cut = _ctx.Render<SeasonTimeline>(parameters => parameters
            .Add(p => p.Season, DateTime.Today.Year.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Add(p => p.Tournaments, [tournament]));

        var dot = cut.Find(".season-timeline__dot");
        dot.TriggerEvent("onmouseover", new MouseEventArgs());

        cut.Find(".season-timeline__tooltip").TextContent.ShouldContain(tournament.Name);

        dot.TriggerEvent("onmouseout", new MouseEventArgs());

        cut.FindAll(".season-timeline__tooltip").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render merged season month order when season is 2020-21")]
    public void Render_ShouldUseMergedMonthLabels_WhenSeasonIsMerged()
    {
        var tournament = SeasonTournamentViewModelFactory.Create(season: "2020-21");

        var cut = _ctx.Render<SeasonTimeline>(parameters => parameters
            .Add(p => p.Season, "2020-21")
            .Add(p => p.Tournaments, [tournament]));

        cut.Markup.ShouldContain("Sep");
        cut.Markup.ShouldContain("Aug");
    }
}