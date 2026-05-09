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

    [Fact(DisplayName = "Should render today marker when today falls within the season")]
    public void Render_ShouldShowTodayMarker_WhenTodayIsInSeason()
    {
        var currentYear = DateTime.Today.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var tournament = SeasonTournamentViewModelFactory.Create(season: currentYear);

        var cut = _ctx.Render<SeasonTimeline>(parameters => parameters
            .Add(p => p.Season, currentYear)
            .Add(p => p.Tournaments, [tournament]));

        cut.FindAll(".season-timeline__today-marker").Count.ShouldBe(1);
        cut.Markup.ShouldContain("Today");
    }

    [Fact(DisplayName = "Should not render today marker when season is in the past")]
    public void Render_ShouldNotShowTodayMarker_WhenSeasonIsInThePast()
    {
        var pastYear = (DateTime.Today.Year - 2).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var tournament = SeasonTournamentViewModelFactory.Create(season: pastYear);

        var cut = _ctx.Render<SeasonTimeline>(parameters => parameters
            .Add(p => p.Season, pastYear)
            .Add(p => p.Tournaments, [tournament]));

        cut.FindAll(".season-timeline__today-marker").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should render completed dot class for past tournament and upcoming class for future")]
    public void Render_ShouldApplyCorrectDotClass_BasedOnTournamentStatus()
    {
        var currentYear = DateTime.Today.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var pastDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var futureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30));

        var pastTournament = SeasonTournamentViewModelFactory.Create(
            season: currentYear, startDate: pastDate, endDate: pastDate, name: "Past Open");
        var futureTournament = SeasonTournamentViewModelFactory.Create(
            season: currentYear, startDate: futureDate, endDate: futureDate, name: "Future Open");

        var cut = _ctx.Render<SeasonTimeline>(parameters => parameters
            .Add(p => p.Season, currentYear)
            .Add(p => p.Tournaments, [pastTournament, futureTournament]));

        cut.FindAll(".season-timeline__dot--completed").Count.ShouldBe(1);
        cut.FindAll(".season-timeline__dot--upcoming").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should render empty timeline without crashing when no tournaments provided")]
    public void Render_ShouldRenderEmptyTimeline_WhenTournamentsIsEmpty()
    {
        var currentYear = DateTime.Today.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var cut = _ctx.Render<SeasonTimeline>(parameters => parameters
            .Add(p => p.Season, currentYear)
            .Add(p => p.Tournaments, []));

        cut.FindAll(".season-timeline__dot").Count.ShouldBe(0);
        cut.Find(".season-timeline").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should show tooltip on keyboard focus and hide on blur")]
    public void Tooltip_ShouldShowAndHide_WhenDotFocusedThenBlurred()
    {
        var currentYear = DateTime.Today.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var tournament = SeasonTournamentViewModelFactory.Create(season: currentYear);

        var cut = _ctx.Render<SeasonTimeline>(parameters => parameters
            .Add(p => p.Season, currentYear)
            .Add(p => p.Tournaments, [tournament]));

        var dot = cut.Find(".season-timeline__dot");
        dot.TriggerEvent("onfocus", new Microsoft.AspNetCore.Components.Web.FocusEventArgs());

        cut.Find(".season-timeline__tooltip").TextContent.ShouldContain(tournament.Name);

        dot.TriggerEvent("onblur", new Microsoft.AspNetCore.Components.Web.FocusEventArgs());

        cut.FindAll(".season-timeline__tooltip").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should include tournament name and status in dot aria-label")]
    public void Render_ShouldIncludeNameAndStatusInDotAriaLabel()
    {
        var currentYear = DateTime.Today.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var futureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        var tournament = SeasonTournamentViewModelFactory.Create(
            season: currentYear, startDate: futureDate, endDate: futureDate, name: "Spring Open");

        var cut = _ctx.Render<SeasonTimeline>(parameters => parameters
            .Add(p => p.Season, currentYear)
            .Add(p => p.Tournaments, [tournament]));

        var dot = cut.Find(".season-timeline__dot");
        var ariaLabel = dot.GetAttribute("aria-label") ?? string.Empty;

        ariaLabel.ShouldContain("Spring Open");
        ariaLabel.ShouldContain("upcoming");
    }
}