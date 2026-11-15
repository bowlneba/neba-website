using Neba.Api.Contracts.Memberships;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;

namespace Neba.Api.Tests.Features.HallOfFame;

[UnitTest]
[Component("HallOfFame")]
public sealed class HallOfFamePointsCalculatorTests
{
    private readonly HallOfFamePointsCalculator _calculator = new();

    private static readonly BowlerId Bowler = new("01JZ8K3M4N5P6Q7R8S9T0V1W2X");
    private static readonly BowlerId OtherBowler = new("01JZ8K3M4N5P6Q7R8S9T0V1W2Y");

    private static SeasonId Season(int n) => new($"01JZ8K3M4N5P6Q7R8S9T0V1W{n:00}");

    private static TitleWin Title(BowlerId bowler, int season, TournamentType type, int year = 2019)
        => TitleWinFactory.Create(
            bowlerId: bowler,
            seasonId: Season(season),
            startDate: new DateOnly(year, 3, 9),
            endDate: new DateOnly(year, 3, 10),
            tournamentType: type);

    // ---- points ----

    [Fact(DisplayName = "Sums titles and awards for one bowler")]
    public void CalculatePoints_ShouldSumTitlesAndAwards()
    {
        // Arrange
        var titles = new[] { Title(Bowler, 1, TournamentType.Singles), Title(Bowler, 2, TournamentType.Senior) };
        var awards = new[] { AwardWinFactory.Create(bowlerId: Bowler, seasonId: Season(3), category: BowlerOfTheYearCategory.Open) };

        // Act
        var result = _calculator.CalculatePoints(titles, awards);

        // Assert
        var points = result.ShouldHaveSingleItem();
        points.BowlerId.ShouldBe(Bowler);
        points.TotalPoints.ShouldBe(6 + 2 + 12);
    }

    [Fact(DisplayName = "A bowler with only zero-point wins is absent")]
    public void CalculatePoints_ShouldOmitBowler_WhenOnlyZeroPointWins()
    {
        // Arrange
        var titles = new[] { Title(Bowler, 1, TournamentType.Youth), Title(Bowler, 2, TournamentType.Baker) };
        var awards = new[] { AwardWinFactory.Create(bowlerId: Bowler, category: BowlerOfTheYearCategory.Youth) };

        // Act
        var result = _calculator.CalculatePoints(titles, awards);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Counts distinct seasons, so two titles in one season count once")]
    public void CalculatePoints_ShouldCountDistinctSeasons()
    {
        // Arrange
        var titles = new[]
        {
            Title(Bowler, 1, TournamentType.Singles),
            Title(Bowler, 1, TournamentType.Doubles),
            Title(Bowler, 2, TournamentType.Masters)
        };

        // Act
        var result = _calculator.CalculatePoints(titles, []);

        // Assert
        result.Single().SeasonsWithPoints.ShouldBe(2);
    }

    [Fact(DisplayName = "A zero-point win does not add a season")]
    public void CalculatePoints_ShouldNotCountSeason_ForZeroPointWin()
    {
        // Arrange
        var titles = new[] { Title(Bowler, 1, TournamentType.Singles), Title(Bowler, 2, TournamentType.Youth) };

        // Act
        var result = _calculator.CalculatePoints(titles, []);

        // Assert
        result.Single().SeasonsWithPoints.ShouldBe(1);
    }

    [Fact(DisplayName = "Breakdown is oldest first")]
    public void CalculatePoints_ShouldOrderBreakdownOldestFirst()
    {
        // Arrange
        var titles = new[]
        {
            Title(Bowler, 3, TournamentType.Doubles, year: 2022),
            Title(Bowler, 1, TournamentType.Singles, year: 2018),
            Title(Bowler, 2, TournamentType.Masters, year: 2020)
        };

        // Act
        var result = _calculator.CalculatePoints(titles, []);

        // Assert
        result.Single().Breakdown.Select(entry => entry.Date.Year).ShouldBe([2018, 2020, 2022]);
    }

    [Fact(DisplayName = "Running total adds up oldest first")]
    public void CalculatePoints_ShouldGiveRunningTotals_OldestFirst()
    {
        // Arrange
        var titles = new[]
        {
            Title(Bowler, 2, TournamentType.Doubles, year: 2020),
            Title(Bowler, 1, TournamentType.Senior, year: 2018),
            Title(Bowler, 3, TournamentType.Masters, year: 2022)
        };

        // Act
        var result = _calculator.CalculatePoints(titles, []);

        // Assert
        result.Single().Breakdown.Select(entry => entry.RunningTotal).ShouldBe([2, 8, 14]);
    }

    [Fact(DisplayName = "Marks only the first entry that reaches the required points")]
    public void CalculatePoints_ShouldMarkOnlyTheEntryThatReachesThreshold()
    {
        // Arrange - 12 + 12 + 6 + 6 = 36 on the fourth entry, then 6 more
        var titles = new[]
        {
            Title(Bowler, 1, TournamentType.Singles, year: 1988),
            Title(Bowler, 2, TournamentType.Singles, year: 1989),
            Title(Bowler, 3, TournamentType.Doubles, year: 2005),
            Title(Bowler, 4, TournamentType.Masters, year: 2006),
            Title(Bowler, 5, TournamentType.Invitational, year: 2007)
        };

        // Act
        var breakdown = _calculator.CalculatePoints(titles, []).Single().Breakdown;

        // Assert
        breakdown.Select(entry => entry.ReachedThreshold).ShouldBe([false, false, false, true, false]);
        breakdown[3].RunningTotal.ShouldBe(36);
        breakdown[4].RunningTotal.ShouldBe(42);
    }

    [Fact(DisplayName = "Marks the entry that jumps past the required points")]
    public void CalculatePoints_ShouldMarkEntryThatJumpsPastThreshold()
    {
        // Arrange - 30 points, then a 12-point award takes it to 42
        var titles = new[]
        {
            Title(Bowler, 1, TournamentType.Doubles, year: 2005),
            Title(Bowler, 2, TournamentType.Masters, year: 2006),
            Title(Bowler, 3, TournamentType.Invitational, year: 2007),
            Title(Bowler, 4, TournamentType.Singles, year: 2008),
            Title(Bowler, 5, TournamentType.TournamentOfChampions, year: 2009)
        };
        var awards = new[] { AwardWinFactory.Create(bowlerId: Bowler, seasonId: Season(6), seasonEndDate: new DateOnly(2010, 12, 31), category: BowlerOfTheYearCategory.Open) };

        // Act
        var breakdown = _calculator.CalculatePoints(titles, awards).Single().Breakdown;

        // Assert
        var reached = breakdown.Single(entry => entry.ReachedThreshold);
        reached.Source.ShouldBe("Bowler of the Year");
        reached.RunningTotal.ShouldBe(42);
    }

    [Fact(DisplayName = "Marks nothing for a bowler under the required points")]
    public void CalculatePoints_ShouldMarkNothing_WhenUnderThreshold()
    {
        // Arrange
        var titles = new[] { Title(Bowler, 1, TournamentType.Singles), Title(Bowler, 2, TournamentType.Doubles) };

        // Act
        var breakdown = _calculator.CalculatePoints(titles, []).Single().Breakdown;

        // Assert
        breakdown.ShouldAllBe(entry => !entry.ReachedThreshold);
    }

    [Fact(DisplayName = "Pre-1991 Singles uses the legacy label and 12 points")]
    public void CalculatePoints_ShouldUseLegacyRule_ForSinglesBefore1991()
    {
        // Arrange
        var titles = new[] { Title(Bowler, 1, TournamentType.Singles, year: 1988) };

        // Act
        var result = _calculator.CalculatePoints(titles, []);

        // Assert
        var entry = result.Single().Breakdown.Single();
        entry.Points.ShouldBe(12);
        entry.Source.ShouldBe("NEBA Singles Title (prior to 1991)");
    }

    [Fact(DisplayName = "Keeps each bowler's points apart")]
    public void CalculatePoints_ShouldGroupByBowler()
    {
        // Arrange
        var titles = new[] { Title(Bowler, 1, TournamentType.Singles), Title(OtherBowler, 1, TournamentType.Trios) };

        // Act
        var result = _calculator.CalculatePoints(titles, []);

        // Assert
        result.Count.ShouldBe(2);
        result.Single(points => points.BowlerId == Bowler).TotalPoints.ShouldBe(6);
        result.Single(points => points.BowlerId == OtherBowler).TotalPoints.ShouldBe(2);
    }

    [Theory(DisplayName = "MeetsPoints is false at 35 and true at 36")]
    [InlineData(35, false, TestDisplayName = "35 points")]
    [InlineData(36, true, TestDisplayName = "36 points")]
    public void BowlerPoints_MeetsPoints_ShouldCompareToRequired(int total, bool expected)
    {
        // Arrange
        var points = new BowlerPoints(Bowler, total, 3, []);

        // Act & Assert
        points.MeetsPoints.ShouldBe(expected);
    }

    [Theory(DisplayName = "MeetsSeasons is false at 2 seasons and true at 3")]
    [InlineData(2, false, TestDisplayName = "2 seasons")]
    [InlineData(3, true, TestDisplayName = "3 seasons")]
    public void BowlerPoints_MeetsSeasons_ShouldCompareToRequired(int seasons, bool expected)
    {
        // Arrange
        var points = new BowlerPoints(Bowler, 36, seasons, []);

        // Act & Assert
        points.MeetsSeasons.ShouldBe(expected);
    }

    [Fact(DisplayName = "CalculatePoints throws on null input")]
    public void CalculatePoints_ShouldThrow_WhenInputIsNull()
    {
#nullable disable
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _calculator.CalculatePoints(null, []));
        Should.Throw<ArgumentNullException>(() => _calculator.CalculatePoints([], null));
#nullable enable
    }

    // ---- memberships ----

    private static MembershipRecord Membership(MembershipType type, int year, BowlerId? bowler = null, int month = 12, int day = 31)
        => MembershipRecordFactory.Create(bowlerId: bowler ?? Bowler, type: type, endDate: new DateOnly(year, month, day));

    [Fact(DisplayName = "Counts only the types that count toward the Hall of Fame")]
    public void SummarizeMemberships_ShouldCountOnlyCountingTypes()
    {
        // Arrange
        var records = new[]
        {
            Membership(MembershipType.New, 2010),
            Membership(MembershipType.Renewal, 2011),
            Membership(MembershipType.Guest, 2012),
            Membership(MembershipType.NonMember, 2013)
        };

        // Act
        var summary = _calculator.SummarizeMemberships(records)[Bowler];

        // Assert
        summary.Years.Select(year => year.Year).ShouldBe([2010, 2011]);
    }

    [Fact(DisplayName = "Two memberships ending in the same year give one year")]
    public void SummarizeMemberships_ShouldGiveOneYear_ForTwoMembershipsInSameYear()
    {
        // Arrange
        var records = new[]
        {
            Membership(MembershipType.New, 2010, month: 3, day: 1),
            Membership(MembershipType.Renewal, 2010, month: 11, day: 1)
        };

        // Act
        var summary = _calculator.SummarizeMemberships(records)[Bowler];

        // Assert
        var year = summary.Years.ShouldHaveSingleItem();
        year.Year.ShouldBe(2010);
        year.Type.ShouldBe(MembershipType.New);
    }

    [Fact(DisplayName = "A November purchase counts for the next year")]
    public void SummarizeMemberships_ShouldCountNextYear_ForNovemberPurchase()
    {
        // Arrange - bought in November 2019, ends in 2020
        var records = new[] { Membership(MembershipType.Renewal, 2020, month: 12, day: 31) };

        // Act
        var summary = _calculator.SummarizeMemberships(records)[Bowler];

        // Assert
        summary.Years.Single().Year.ShouldBe(2020);
    }

    [Fact(DisplayName = "Years are oldest first")]
    public void SummarizeMemberships_ShouldOrderYearsOldestFirst()
    {
        // Arrange
        var records = new[]
        {
            Membership(MembershipType.Renewal, 2015),
            Membership(MembershipType.New, 2012),
            Membership(MembershipType.Renewal, 2013)
        };

        // Act
        var summary = _calculator.SummarizeMemberships(records)[Bowler];

        // Assert
        summary.Years.Select(year => year.Year).ShouldBe([2012, 2013, 2015]);
    }

    [Fact(DisplayName = "History is complete when a New record is on file")]
    public void SummarizeMemberships_ShouldBeComplete_WhenNewRecordExists()
    {
        // Arrange
        var records = new[] { Membership(MembershipType.New, 2010), Membership(MembershipType.Renewal, 2011) };

        // Act
        var summary = _calculator.SummarizeMemberships(records)[Bowler];

        // Assert
        summary.HistoryComplete.ShouldBeTrue();
    }

    [Fact(DisplayName = "History is incomplete with only Renewal records")]
    public void SummarizeMemberships_ShouldBeIncomplete_WhenOnlyRenewals()
    {
        // Arrange
        var records = new[] { Membership(MembershipType.Renewal, 2010), Membership(MembershipType.Renewal, 2011) };

        // Act
        var summary = _calculator.SummarizeMemberships(records)[Bowler];

        // Assert
        summary.HistoryComplete.ShouldBeFalse();
    }

    [Fact(DisplayName = "History is incomplete with Renewal plus Guest records and no New")]
    public void SummarizeMemberships_ShouldBeIncomplete_WhenRenewalAndGuestWithoutNew()
    {
        // Arrange
        var records = new[] { Membership(MembershipType.Renewal, 2010), Membership(MembershipType.Guest, 2011) };

        // Act
        var summary = _calculator.SummarizeMemberships(records)[Bowler];

        // Assert
        summary.HistoryComplete.ShouldBeFalse();
    }

    [Theory(DisplayName = "History is complete with only Guest or Non-Member records")]
    [InlineData("GUEST", TestDisplayName = "Guest only")]
    [InlineData("NON_MEMBER", TestDisplayName = "Non-Member only")]
    public void SummarizeMemberships_ShouldBeComplete_WhenNoCountingRecords(string code)
    {
        // Arrange
        var records = new[] { Membership(MembershipType.FromValue(code), 2015) };

        // Act
        var summary = _calculator.SummarizeMemberships(records)[Bowler];

        // Assert
        summary.Years.ShouldBeEmpty();
        summary.HistoryComplete.ShouldBeTrue();
    }

    [Fact(DisplayName = "A bowler with no records is absent, and None gives 0 years and complete")]
    public void SummarizeMemberships_ShouldOmitBowler_WhenNoRecords()
    {
        // Act
        var result = _calculator.SummarizeMemberships([]);

        // Assert
        result.ShouldBeEmpty();
        MembershipSummary.None.Years.ShouldBeEmpty();
        MembershipSummary.None.HistoryComplete.ShouldBeTrue();
    }

    [Fact(DisplayName = "A guest winner who never joined has 0 membership years and a complete history")]
    public void SummarizeMemberships_ShouldGiveZeroYears_ForGuestWinner()
    {
        // Arrange
        var titles = new[] { Title(Bowler, 1, TournamentType.Singles) };
        var records = new[] { Membership(MembershipType.Guest, 2019) };

        // Act
        var points = _calculator.CalculatePoints(titles, []).Single();
        var summary = _calculator.SummarizeMemberships(records).GetValueOrDefault(Bowler, MembershipSummary.None);

        // Assert
        points.TotalPoints.ShouldBe(6);
        points.SeasonsWithPoints.ShouldBe(1);
        summary.Years.Count.ShouldBe(0);
        summary.HistoryComplete.ShouldBeTrue();
    }

    [Fact(DisplayName = "A guest winner who later buys a Renewal with no New has 1 year and an incomplete history")]
    public void SummarizeMemberships_ShouldGiveOneYearAndIncomplete_ForGuestWinnerWithLaterRenewal()
    {
        // Arrange
        var records = new[] { Membership(MembershipType.Guest, 2019), Membership(MembershipType.Renewal, 2021) };

        // Act
        var summary = _calculator.SummarizeMemberships(records)[Bowler];

        // Assert
        summary.Years.Count.ShouldBe(1);
        summary.HistoryComplete.ShouldBeFalse();
    }

    [Fact(DisplayName = "SummarizeMemberships throws on null input")]
    public void SummarizeMemberships_ShouldThrow_WhenInputIsNull()
    {
#nullable disable
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _calculator.SummarizeMemberships(null));
#nullable enable
    }
}