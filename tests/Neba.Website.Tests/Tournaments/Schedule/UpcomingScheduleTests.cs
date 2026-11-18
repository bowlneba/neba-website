using Neba.Api.Contracts.Series.GetSeries;
using Neba.Api.Contracts.Series.ListSeriesFinales;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.UpcomingSchedule")]
public sealed class UpcomingScheduleTests
{
    private static readonly SeasonViewModel Season2026 = SeasonViewModelFactory.Create(id: "s26", description: "2026 Season", startDate: new DateOnly(2026, 1, 1), endDate: new DateOnly(2026, 12, 31));
    private static readonly SeasonViewModel Season2027 = SeasonViewModelFactory.Create(id: "s27", description: "2027 Season", startDate: new DateOnly(2027, 1, 1), endDate: new DateOnly(2027, 12, 31));

    private static SeasonTournamentViewModel Upcoming(string id, string season)
        => SeasonTournamentViewModelFactory.Create(
            id: id,
            season: season,
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(5)));

    private static SeasonTournamentViewModel Past(string id, string season)
        => SeasonTournamentViewModelFactory.Create(
            id: id,
            season: season,
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-9)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-8)));

    [Fact(DisplayName = "GroupBySeason should put a divider above every season except the leading one")]
    public void GroupBySeason_ShouldDivideEverySeasonExceptTheLeadingOne()
    {
        // Arrange
        List<SeasonTournamentViewModel> tournaments = [Upcoming("a", "2026"), Upcoming("b", "2026"), Upcoming("c", "2027")];

        // Act
        var sections = UpcomingSchedule.GroupBySeason(tournaments, [Season2026, Season2027], leadingSeasonLabel: "2026");

        // Assert
        sections.Select(s => (s.Season.Label, s.Tournaments.Count, s.ShowDivider))
            .ShouldBe([("2026", 2, false), ("2027", 1, true)]);
    }

    [Fact(DisplayName = "GroupBySeason should show no divider when the list starts in the only season it holds")]
    public void GroupBySeason_ShouldShowNoDivider_WhenListHoldsOneSeason()
    {
        // Arrange
        List<SeasonTournamentViewModel> tournaments = [Upcoming("c", "2027")];

        // Act
        var sections = UpcomingSchedule.GroupBySeason(tournaments, [Season2026, Season2027], leadingSeasonLabel: "2027");

        // Assert
        sections.ShouldHaveSingleItem().ShowDivider.ShouldBeFalse();
    }

    [Fact(DisplayName = "GroupBySeason should keep a divider on a later season when the leading season is filtered out")]
    public void GroupBySeason_ShouldKeepDivider_WhenLeadingSeasonIsFilteredOut()
    {
        // Arrange
        List<SeasonTournamentViewModel> filtered = [Upcoming("c", "2027")];

        // Act
        var sections = UpcomingSchedule.GroupBySeason(filtered, [Season2026, Season2027], leadingSeasonLabel: "2026");

        // Assert
        sections.ShouldHaveSingleItem().ShowDivider.ShouldBeTrue();
    }

    [Fact(DisplayName = "GroupBySeason should return no sections for an empty list")]
    public void GroupBySeason_ShouldReturnNoSections_WhenListIsEmpty()
    {
        // Arrange
        List<SeasonTournamentViewModel> tournaments = [];

        // Act
        var sections = UpcomingSchedule.GroupBySeason(tournaments, [Season2026], leadingSeasonLabel: null);

        // Assert
        sections.ShouldBeEmpty();
    }

    [Fact(DisplayName = "GroupBySeason should describe a season missing from the season list from its tournament dates")]
    public void GroupBySeason_ShouldDescribeUnknownSeason_FromTournamentDates()
    {
        // Arrange
        List<SeasonTournamentViewModel> tournaments = [Upcoming("x", "2030")];

        // Act
        var sections = UpcomingSchedule.GroupBySeason(tournaments, [Season2026], leadingSeasonLabel: "2026");

        // Assert
        var section = sections.ShouldHaveSingleItem();
        section.Season.Description.ShouldBe("2030 Season");
        section.Season.StartDate.ShouldBe(tournaments[0].StartDate);
    }

    [Fact(DisplayName = "BuildTimelineStrips should give the active season a full strip and each later season an upcoming-only strip")]
    public void BuildTimelineStrips_ShouldStackActiveAndLaterSeasons_WhenActiveSeasonHasUpcoming()
    {
        // Arrange
        List<SeasonTournamentViewModel> active = [Past("p", "2026"), Upcoming("u", "2026")];
        List<SeasonTournamentViewModel> upcoming = [Upcoming("u", "2026"), Upcoming("n", "2027")];

        // Act
        var strips = UpcomingSchedule.BuildTimelineStrips(Season2026, active, upcoming, [Season2026, Season2027]);

        // Assert
        strips.Select(s => (s.Season.Label, s.Tournaments.Count, s.UpcomingOnly))
            .ShouldBe([("2026", 2, false), ("2027", 1, true)]);
    }

    [Fact(DisplayName = "BuildTimelineStrips should return only the next season's strip when the active season has nothing upcoming")]
    public void BuildTimelineStrips_ShouldReturnOnlyNextSeason_WhenActiveSeasonIsFinished()
    {
        // Arrange
        List<SeasonTournamentViewModel> active = [Past("p", "2026")];
        List<SeasonTournamentViewModel> upcoming = [Upcoming("n", "2027")];

        // Act
        var strips = UpcomingSchedule.BuildTimelineStrips(Season2026, active, upcoming, [Season2026, Season2027]);

        // Assert
        var strip = strips.ShouldHaveSingleItem();
        strip.Season.Label.ShouldBe("2027");
        strip.UpcomingOnly.ShouldBeTrue();
    }

    [Fact(DisplayName = "BuildTimelineStrips should keep the active season's strip when nothing is upcoming anywhere")]
    public void BuildTimelineStrips_ShouldKeepActiveSeason_WhenNothingIsUpcoming()
    {
        // Arrange
        List<SeasonTournamentViewModel> active = [Past("p", "2026")];

        // Act
        var strips = UpcomingSchedule.BuildTimelineStrips(Season2026, active, [], [Season2026, Season2027]);

        // Assert
        var strip = strips.ShouldHaveSingleItem();
        strip.Season.Label.ShouldBe("2026");
        strip.UpcomingOnly.ShouldBeFalse();
    }

    [Fact(DisplayName = "ResolveSeasonLabel should return the label of the season containing the date")]
    public void ResolveSeasonLabel_ShouldReturnContainingSeasonLabel()
    {
        // Arrange
        var date = new DateOnly(2027, 2, 6);

        // Act
        var label = UpcomingSchedule.ResolveSeasonLabel(date, [Season2026, Season2027]);

        // Assert
        label.ShouldBe("2027");
    }

    [Fact(DisplayName = "ResolveSeasonLabel should fall back to the year when no season contains the date")]
    public void ResolveSeasonLabel_ShouldFallBackToYear_WhenNoSeasonContainsDate()
    {
        // Arrange
        var date = new DateOnly(2031, 2, 6);

        // Act
        var label = UpcomingSchedule.ResolveSeasonLabel(date, [Season2026, Season2027]);

        // Assert
        label.ShouldBe("2031");
    }

    [Fact(DisplayName = "Merge should order tournaments and Finales together by date, ascending")]
    public void Merge_ShouldOrderAscending()
    {
        // Arrange
        var soon = Upcoming("soon", "2026");
        var finale = SeriesFinaleItemViewModelFactory.Create(date: DateOnly.FromDateTime(DateTime.Today.AddDays(8)));
        var later = SeasonTournamentViewModelFactory.Create(
            id: "later", season: "2026",
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(20)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(20)));

        // Act
        var entries = UpcomingSchedule.Merge([later, soon], [finale], newestFirst: false);

        // Assert
        entries.Select(e => e.Tournament?.Id ?? "finale").ShouldBe(["soon", "finale", "later"]);
    }

    [Fact(DisplayName = "Merge should order newest first when asked")]
    public void Merge_ShouldOrderNewestFirst()
    {
        // Arrange
        var old = Past("old", "2026");
        var finale = SeriesFinaleItemViewModelFactory.Create(date: DateOnly.FromDateTime(DateTime.Today.AddDays(-3)));

        // Act
        var entries = UpcomingSchedule.Merge([old], [finale], newestFirst: true);

        // Assert
        entries.Select(e => e.Tournament?.Id ?? "finale").ShouldBe(["finale", "old"]);
    }

    [Fact(DisplayName = "Merge should put a tournament before a Finale on the same day")]
    public void Merge_ShouldPutTournamentFirstOnSameDay()
    {
        // Arrange
        var tournament = Upcoming("t", "2026");
        var finale = SeriesFinaleItemViewModelFactory.Create(date: tournament.StartDate);

        // Act
        var entries = UpcomingSchedule.Merge([tournament], [finale], newestFirst: false);

        // Assert
        entries[0].Tournament.ShouldNotBeNull();
        entries[1].Finale.ShouldNotBeNull();
    }

    [Fact(DisplayName = "GroupEntriesBySeason should put a Finale under the season of its Series")]
    public void GroupEntriesBySeason_ShouldGroupFinaleBySeasonId()
    {
        // Arrange
        var tournament = Upcoming("t", Season2026.Label);
        var finale = SeriesFinaleItemViewModelFactory.Create(seasonId: Season2027.Id, date: new DateOnly(2027, 3, 1));
        var entries = UpcomingSchedule.Merge([tournament], [finale], newestFirst: false);

        // Act
        var sections = UpcomingSchedule.GroupEntriesBySeason(entries, [Season2026, Season2027], Season2026.Label);

        // Assert
        sections.Count.ShouldBe(2);
        sections[0].Season.ShouldBe(Season2026);
        sections[0].ShowDivider.ShouldBeFalse();
        sections[1].Season.ShouldBe(Season2027);
        sections[1].ShowDivider.ShouldBeTrue();
        sections[1].Entries.Single().Finale.ShouldNotBeNull();
    }

    [Fact(DisplayName = "GroupEntriesBySeason should describe a season missing from the list from the entry dates")]
    public void GroupEntriesBySeason_ShouldFallBack_WhenSeasonUnknown()
    {
        // Arrange
        var finale = SeriesFinaleItemViewModelFactory.Create(seasonId: "unknown", date: new DateOnly(2030, 5, 1));
        var entries = UpcomingSchedule.Merge([], [finale], newestFirst: false);

        // Act
        var sections = UpcomingSchedule.GroupEntriesBySeason(entries, [Season2026], null);

        // Assert
        sections.Single().Season.Description.ShouldBe("2030 Season");
    }

    [Fact(DisplayName = "ToFinaleItem should map the summary and build the host line")]
    public void ToFinaleItem_ShouldMapSummary()
    {
        // Arrange
        var response = SeriesFinaleSummaryResponseFactory.Create(
            finale: SeriesFinaleResponseFactory.Create(
                bowlingCenterName: "Callahan's Bowl-O-Rama", bowlingCenterCity: "Seekonk", bowlingCenterState: "MA"));

        // Act
        var item = response.ToFinaleItem();

        // Assert
        item.SeriesId.ShouldBe(response.SeriesId);
        item.SeasonId.ShouldBe(response.SeasonId);
        item.FinalistCount.ShouldBe(response.Finale.FinalistCount);
        item.Host.ShouldBe("Callahan's Bowl-O-Rama, Seekonk, MA");
        item.Title.ShouldBe(response.SeriesName + " Finale");
        item.Href.ShouldBe("/series/" + response.SeriesId);
    }
}