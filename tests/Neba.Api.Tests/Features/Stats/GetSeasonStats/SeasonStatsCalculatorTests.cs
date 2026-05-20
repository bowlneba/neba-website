using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Stats;

namespace Neba.Api.Tests.Features.Stats.GetSeasonStats;

[UnitTest]
[Component("Stats")]
public sealed class SeasonStatsCalculatorTests
{
    private readonly SeasonStatsCalculator _calculator = new();

    // ─── CalculateStatMinimums ────────────────────────────────────────────────

    [Theory(DisplayName = "CalculateStatMinimums should return correct minimums for tournament count")]
    [InlineData(0, 0.0, 0.0, 0.0)]
    [InlineData(2, 9.0, 1.0, 1.5)]
    [InlineData(10, 45.0, 5.0, 7.5)]
    [InlineData(20, 90.0, 10.0, 15.0)]
    public void CalculateStatMinimums_ShouldReturnCorrectValues_ForTournamentCount(
        int tournamentCount, double expectedGames, double expectedTournaments, double expectedEntries)
    {
        var (games, tournaments, entries) = _calculator.CalculateStatMinimums(tournamentCount);

        games.ShouldBe((decimal)expectedGames);
        tournaments.ShouldBe((decimal)expectedTournaments);
        entries.ShouldBe((decimal)expectedEntries);
    }

    // ─── HighAverageLeaderboard ───────────────────────────────────────────────

    [Fact(DisplayName = "HighAverageLeaderboard should be ordered by descending average")]
    public void HighAverageLeaderboard_ShouldBeOrderedByDescendingAverage()
    {
        var higherAvgBowler = BowlerSeasonStatsDtoFactory.Create(totalGames: 100, totalPinfall: 22000);
        var lowerAvgBowler = BowlerSeasonStatsDtoFactory.Create(totalGames: 100, totalPinfall: 19000);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [lowerAvgBowler, higherAvgBowler],
            minimumGames: 50m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.HighAverageLeaderboard.First().Average.ShouldBe(220.00m);
        summary.HighAverageLeaderboard.Last().Average.ShouldBe(190.00m);
    }

    [Fact(DisplayName = "HighAverageLeaderboard Average should equal TotalPinfall divided by TotalGames rounded to 2 places")]
    public void HighAverageLeaderboard_Average_ShouldEqualTotalPinfallDividedByTotalGames()
    {
        // 17000 / 80 = 212.5 → rounds to 212.50
        var bowler = BowlerSeasonStatsDtoFactory.Create(totalGames: 80, totalPinfall: 17000);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [bowler], minimumGames: 1m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.HighAverageLeaderboard.Single().Average.ShouldBe(212.50m);
    }

    [Fact(DisplayName = "HighAverageLeaderboard should exclude bowlers with zero games")]
    public void HighAverageLeaderboard_ShouldExcludeBowlersWithZeroGames()
    {
        var activeBowler = BowlerSeasonStatsDtoFactory.Create(totalGames: 100, totalPinfall: 20000);
        var inactiveBowler = BowlerSeasonStatsDtoFactory.Create(totalGames: 0, totalPinfall: 0);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [activeBowler, inactiveBowler],
            minimumGames: 1m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.HighAverageLeaderboard.Count.ShouldBe(1);
        summary.HighAverageLeaderboard.Single().Games.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "HighAverageLeaderboard should exclude bowlers below minimum game threshold")]
    public void HighAverageLeaderboard_ShouldExcludeBowlersBelowMinimumGameThreshold()
    {
        var qualifyingBowler = BowlerSeasonStatsDtoFactory.Create(totalGames: 100, totalPinfall: 20000);
        var belowThresholdBowler = BowlerSeasonStatsDtoFactory.Create(totalGames: 49, totalPinfall: 9800);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [qualifyingBowler, belowThresholdBowler],
            minimumGames: 50m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.HighAverageLeaderboard.Count.ShouldBe(1);
        summary.HighAverageLeaderboard.Single().Games.ShouldBe(100);
    }

    // ─── HighBlockLeaderboard ────────────────────────────────────────────────

    [Fact(DisplayName = "HighBlockLeaderboard should be ordered by descending high block score")]
    public void HighBlockLeaderboard_ShouldBeOrderedByDescendingHighBlock()
    {
        var higherBlockBowler = BowlerSeasonStatsDtoFactory.Create(highBlock: 1350);
        var lowerBlockBowler = BowlerSeasonStatsDtoFactory.Create(highBlock: 1100);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [lowerBlockBowler, higherBlockBowler],
            minimumGames: 1m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.HighBlockLeaderboard.First().HighBlock.ShouldBe(1350);
        summary.HighBlockLeaderboard.Last().HighBlock.ShouldBe(1100);
    }

    // ─── MatchPlayRecordLeaderboard ──────────────────────────────────────────

    [Fact(DisplayName = "MatchPlayRecordLeaderboard WinPercentage should equal wins divided by total times 100 rounded to 2 places")]
    public void MatchPlayRecordLeaderboard_WinPercentage_ShouldEqualWinsDividedByTotalTimesOneHundred()
    {
        // 3W / (3+1) * 100 = 75.00%
        var bowler = BowlerSeasonStatsDtoFactory.Create(matchPlayWins: 3, matchPlayLosses: 1, matchPlayGames: 4);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [bowler], minimumGames: 1m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.MatchPlayRecordLeaderboard.Single().WinPercentage.ShouldBe(75.00m);
    }

    [Fact(DisplayName = "MatchPlayRecordLeaderboard should order by wins as tie-breaker when win percentage is equal")]
    public void MatchPlayRecordLeaderboard_ShouldOrderByWinsAsTieBreaker_WhenWinPercentageIsEqual()
    {
        // 2W/2L and 6W/6L both yield 50% — 6W should rank higher
        var fewerWinsBowler = BowlerSeasonStatsDtoFactory.Create(matchPlayWins: 2, matchPlayLosses: 2, matchPlayGames: 4);
        var moreWinsBowler = BowlerSeasonStatsDtoFactory.Create(matchPlayWins: 6, matchPlayLosses: 6, matchPlayGames: 12);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [fewerWinsBowler, moreWinsBowler],
            minimumGames: 1m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.MatchPlayRecordLeaderboard.First().Wins.ShouldBe(6);
        summary.MatchPlayRecordLeaderboard.Last().Wins.ShouldBe(2);
    }

    // ─── MatchPlayAverageLeaderboard ─────────────────────────────────────────

    [Fact(DisplayName = "MatchPlayAverageLeaderboard Average should equal MatchPlayPinfall divided by MatchPlayGames rounded to 2 places")]
    public void MatchPlayAverageLeaderboard_Average_ShouldEqualMatchPlayPinfallDividedByMatchPlayGames()
    {
        // 1760 / 8 = 220.00
        var bowler = BowlerSeasonStatsDtoFactory.Create(
            matchPlayWins: 4, matchPlayLosses: 4, matchPlayGames: 8, matchPlayPinfall: 1760);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [bowler], minimumGames: 1m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.MatchPlayAverageLeaderboard.Single().MatchPlayAverage.ShouldBe(220.00m);
    }

    [Fact(DisplayName = "MatchPlayAverageLeaderboard should exclude bowlers below minimum match play games threshold")]
    public void MatchPlayAverageLeaderboard_ShouldExcludeBowlers_BelowMinimumMatchPlayGamesThreshold()
    {
        // minimumTournaments = 3 → minimumMatchPlayGames = 6
        var qualifyingBowler = BowlerSeasonStatsDtoFactory.Create(
            matchPlayWins: 3, matchPlayLosses: 3, matchPlayGames: 6, matchPlayPinfall: 1200);
        var belowThresholdBowler = BowlerSeasonStatsDtoFactory.Create(
            matchPlayWins: 2, matchPlayLosses: 3, matchPlayGames: 5, matchPlayPinfall: 1000);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [qualifyingBowler, belowThresholdBowler],
            minimumGames: 1m, minimumTournaments: 3m, minimumEntries: 1m);

        summary.MatchPlayAverageLeaderboard.Count.ShouldBe(1);
        summary.MatchPlayAverageLeaderboard.Single().Games.ShouldBe(6);
    }

    // ─── PointsPerEntryLeaderboard ───────────────────────────────────────────

    [Fact(DisplayName = "PointsPerEntryLeaderboard PointsPerEntry should equal BoyPoints divided by EligibleEntries rounded to 2 places")]
    public void PointsPerEntryLeaderboard_PointsPerEntry_ShouldEqualPointsDividedByEligibleEntries()
    {
        // 300 / 4 = 75.00
        var bowler = BowlerSeasonStatsDtoFactory.Create(eligibleEntries: 4, bowlerOfTheYearPoints: 300);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [bowler], minimumGames: 1m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.PointsPerEntryLeaderboard.Single().PointsPerEntry.ShouldBe(75.00m);
    }

    // ─── PointsPerTournamentLeaderboard ──────────────────────────────────────

    [Fact(DisplayName = "PointsPerTournamentLeaderboard PointsPerTournament should equal BoyPoints divided by EligibleTournaments rounded to 2 places")]
    public void PointsPerTournamentLeaderboard_PointsPerTournament_ShouldEqualPointsDividedByEligibleTournaments()
    {
        // 300 / 4 = 75.00
        var bowler = BowlerSeasonStatsDtoFactory.Create(eligibleTournaments: 4, bowlerOfTheYearPoints: 300);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [bowler], minimumGames: 1m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.PointsPerTournamentLeaderboard.Single().PointsPerTournament.ShouldBe(75.00m);
    }

    // ─── FinalsPerEntryLeaderboard ───────────────────────────────────────────

    [Fact(DisplayName = "FinalsPerEntryLeaderboard FinalsPerEntry should equal Finals divided by EligibleEntries rounded to 2 places")]
    public void FinalsPerEntryLeaderboard_FinalsPerEntry_ShouldEqualFinalsDividedByEligibleEntries()
    {
        // 3 / 4 = 0.75
        var bowler = BowlerSeasonStatsDtoFactory.Create(
            eligibleEntries: 4, finals: 3, bowlerOfTheYearPoints: 100);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [bowler], minimumGames: 1m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.FinalsPerEntryLeaderboard.Single().FinalsPerEntry.ShouldBe(0.75m);
    }

    // ─── AverageFinishesLeaderboard ──────────────────────────────────────────

    [Fact(DisplayName = "AverageFinishesLeaderboard should be ordered ascending by average finish position")]
    public void AverageFinishesLeaderboard_ShouldBeOrderedAscending_ByAverageFinishPosition()
    {
        // Lower average finish = better placement — ascending order puts the best on top
        var betterFinishBowler = BowlerSeasonStatsDtoFactory.Create(averageFinish: 2.5m);
        var worseFinishBowler = BowlerSeasonStatsDtoFactory.Create(averageFinish: 5.0m);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [worseFinishBowler, betterFinishBowler],
            minimumGames: 1m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.AverageFinishesLeaderboard.First().AverageFinish.ShouldBe(2.5m);
        summary.AverageFinishesLeaderboard.Last().AverageFinish.ShouldBe(5.0m);
    }

    // ─── AllBowlers ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "AllBowlers should be ordered by descending BowlerOfTheYear points")]
    public void AllBowlers_ShouldBeOrderedByDescendingBowlerOfTheYearPoints()
    {
        var highPointsBowler = BowlerSeasonStatsDtoFactory.Create(bowlerOfTheYearPoints: 800);
        var lowPointsBowler = BowlerSeasonStatsDtoFactory.Create(bowlerOfTheYearPoints: 200);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [lowPointsBowler, highPointsBowler],
            minimumGames: 1m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.AllBowlers.First().Points.ShouldBe(800);
        summary.AllBowlers.Last().Points.ShouldBe(200);
    }

    // ─── BowlerSearchList ────────────────────────────────────────────────────

    [Fact(DisplayName = "BowlerSearchList should be ordered alphabetically by last name then first name")]
    public void BowlerSearchList_ShouldBeOrderedAlphabetically_ByLastNameThenFirstName()
    {
        var zelensky = BowlerSeasonStatsDtoFactory.Create(
            bowlerName: new Name { FirstName = "Volodymyr", LastName = "Zelensky" });
        var andersonAlice = BowlerSeasonStatsDtoFactory.Create(
            bowlerName: new Name { FirstName = "Alice", LastName = "Anderson" });
        var andersonBob = BowlerSeasonStatsDtoFactory.Create(
            bowlerName: new Name { FirstName = "Bob", LastName = "Anderson" });

        var summary = _calculator.CalculateSeasonStatsSummary(
            [zelensky, andersonBob, andersonAlice],
            minimumGames: 1m, minimumTournaments: 1m, minimumEntries: 1m);

        var names = summary.BowlerSearchList.Select(e => e.BowlerName).ToArray();
        names[0].LastName.ShouldBe("Anderson");
        names[0].FirstName.ShouldBe("Alice");
        names[1].LastName.ShouldBe("Anderson");
        names[1].FirstName.ShouldBe("Bob");
        names[2].LastName.ShouldBe("Zelensky");
    }

    // ─── Award Standings ─────────────────────────────────────────────────────

    [Fact(DisplayName = "BowlerOfTheYear standings should be ordered by descending points and exclude zero-point bowlers")]
    public void BowlerOfTheYearStandings_ShouldBeOrderedByDescendingPointsAndExcludeZeroPointBowlers()
    {
        var highPointsBowler = BowlerSeasonStatsDtoFactory.Create(bowlerOfTheYearPoints: 500);
        var lowPointsBowler = BowlerSeasonStatsDtoFactory.Create(bowlerOfTheYearPoints: 100);
        var zeroPointsBowler = BowlerSeasonStatsDtoFactory.Create(bowlerOfTheYearPoints: 0);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [lowPointsBowler, zeroPointsBowler, highPointsBowler],
            minimumGames: 1m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.BowlerOfTheYear.Count.ShouldBe(2);
        summary.BowlerOfTheYear.First().Points.ShouldBe(500);
        summary.BowlerOfTheYear.Last().Points.ShouldBe(100);
    }

    [Fact(DisplayName = "SeniorOfTheYear standings should only include bowlers where IsSenior is true")]
    public void SeniorOfTheYearStandings_ShouldOnlyIncludeSeniorBowlers()
    {
        // Non-senior with higher points should not appear; only the senior should
        var seniorBowler = BowlerSeasonStatsDtoFactory.Create(isSenior: true, seniorOfTheYearPoints: 300);
        var nonSeniorBowler = BowlerSeasonStatsDtoFactory.Create(isSenior: false, seniorOfTheYearPoints: 500);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [seniorBowler, nonSeniorBowler],
            minimumGames: 1m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.SeniorOfTheYear.Count.ShouldBe(1);
        summary.SeniorOfTheYear.Single().Points.ShouldBe(300);
    }

    [Fact(DisplayName = "WomanOfTheYear standings should only include bowlers where IsWoman is true")]
    public void WomanOfTheYearStandings_ShouldOnlyIncludeWomenBowlers()
    {
        var womanBowler = BowlerSeasonStatsDtoFactory.Create(isWoman: true, womanOfTheYearPoints: 400);
        var nonWomanBowler = BowlerSeasonStatsDtoFactory.Create(isWoman: false, womanOfTheYearPoints: 600);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [womanBowler, nonWomanBowler],
            minimumGames: 1m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.WomanOfTheYear.Count.ShouldBe(1);
        summary.WomanOfTheYear.Single().Points.ShouldBe(400);
    }

    // ─── TotalEntries ────────────────────────────────────────────────────────

    [Fact(DisplayName = "TotalEntries should sum TotalEntries across all bowlers not EligibleEntries")]
    public void TotalEntries_ShouldSumTotalEntriesAcrossAllBowlers()
    {
        // EligibleEntries intentionally differ from TotalEntries to prove the right field is summed
        var bowlerA = BowlerSeasonStatsDtoFactory.Create(eligibleEntries: 5, totalEntries: 8);
        var bowlerB = BowlerSeasonStatsDtoFactory.Create(eligibleEntries: 3, totalEntries: 5);

        var summary = _calculator.CalculateSeasonStatsSummary(
            [bowlerA, bowlerB], minimumGames: 1m, minimumTournaments: 1m, minimumEntries: 1m);

        summary.TotalEntries.ShouldBe(13);
    }

    // ─── Smoke test ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return well-formed summary with realistic bogus data")]
    public void CalculateSeasonStatsSummary_ShouldReturnWellFormedSummary_WithRealisticBogusData()
    {
        var bowlerStats = BowlerSeasonStatsDtoFactory.Bogus(count: 20, seed: 42);
        var (minimumGames, minimumTournaments, minimumEntries) = _calculator.CalculateStatMinimums(10);

        var summary = _calculator.CalculateSeasonStatsSummary(
            bowlerStats, minimumGames, minimumTournaments, minimumEntries);

        summary.ShouldNotBeNull();
        summary.TotalEntries.ShouldBeGreaterThanOrEqualTo(0);
        summary.HighAverageLeaderboard.ShouldNotBeNull();
        summary.HighBlockLeaderboard.ShouldNotBeNull();
        summary.MatchPlayRecordLeaderboard.ShouldNotBeNull();
        summary.BowlerSearchList.Count.ShouldBe(bowlerStats.Count);
        summary.AllBowlers.Count.ShouldBe(bowlerStats.Count);
    }
}