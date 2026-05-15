using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Stats.BoyProgression;

namespace Neba.Api.Tests.Features.Stats.GetSeasonStats;

[UnitTest]
[Component("Stats")]
public sealed class BowlerOfTheYearRaceCalculatorTests
{
    private readonly BowlerOfTheYearRaceCalculator _calculator = new();

    // ─── Open race — cumulative progression ──────────────────────────────────

    [Fact(DisplayName = "CalculateAllProgressions should accumulate cumulative points across tournaments for Open race")]
    public void CalculateAllProgressions_ShouldAccumulateCumulativePoints_ForOpenRace()
    {
        var bowlerId = BowlerId.New();
        var t1 = TournamentId.New();
        var t2 = TournamentId.New();

        var results = new[]
        {
            BoyProgressionResultDtoFactory.Create(
                bowlerId: bowlerId, tournamentId: t1,
                tournamentDate: new DateOnly(2025, 1, 1), tournamentEndDate: new DateOnly(2025, 1, 2),
                statsEligible: true, points: 100),
            BoyProgressionResultDtoFactory.Create(
                bowlerId: bowlerId, tournamentId: t2,
                tournamentDate: new DateOnly(2025, 2, 1), tournamentEndDate: new DateOnly(2025, 2, 2),
                statsEligible: true, points: 150),
        };

        var progressions = _calculator.CalculateAllProgressions(results);

        var openSeries = progressions[BowlerOfTheYearCategory.Open.Value];
        openSeries.Count.ShouldBe(1);
        var series = openSeries.Single();
        series.Results.Count.ShouldBe(2);
        series.Results.First().CumulativePoints.ShouldBe(100);
        series.Results.Last().CumulativePoints.ShouldBe(250);
    }

    [Fact(DisplayName = "CalculateAllProgressions should produce independent series per bowler with correct totals")]
    public void CalculateAllProgressions_ShouldProduceIndependentSeriesPerBowler_WithCorrectTotals()
    {
        var bowlerAId = BowlerId.New();
        var bowlerBId = BowlerId.New();
        var t1 = TournamentId.New();
        var t2 = TournamentId.New();

        // Bowler A: 300 pts (t1 only). Bowler B: 100 + 200 = 300 pts. Intentional tie.
        var results = new[]
        {
            BoyProgressionResultDtoFactory.Create(
                bowlerId: bowlerAId, tournamentId: t1,
                tournamentDate: new DateOnly(2025, 1, 1), tournamentEndDate: new DateOnly(2025, 1, 2),
                statsEligible: true, points: 300),
            BoyProgressionResultDtoFactory.Create(
                bowlerId: bowlerBId, tournamentId: t1,
                tournamentDate: new DateOnly(2025, 1, 1), tournamentEndDate: new DateOnly(2025, 1, 2),
                statsEligible: true, points: 100),
            BoyProgressionResultDtoFactory.Create(
                bowlerId: bowlerBId, tournamentId: t2,
                tournamentDate: new DateOnly(2025, 2, 1), tournamentEndDate: new DateOnly(2025, 2, 2),
                statsEligible: true, points: 200),
        };

        var progressions = _calculator.CalculateAllProgressions(results);

        var openSeries = progressions[BowlerOfTheYearCategory.Open.Value];
        openSeries.Count.ShouldBe(2);
        openSeries.ShouldContain(s => s.BowlerId == bowlerAId && s.Results.Last().CumulativePoints == 300);
        openSeries.ShouldContain(s => s.BowlerId == bowlerBId && s.Results.Last().CumulativePoints == 300);
    }

    [Fact(DisplayName = "CalculateAllProgressions should exclude bowler with no stats-eligible results from Open race")]
    public void CalculateAllProgressions_ShouldExcludeBowler_WhenNoStatsEligibleResults()
    {
        var eligibleBowlerId = BowlerId.New();
        var ineligibleBowlerId = BowlerId.New();

        var results = new[]
        {
            BoyProgressionResultDtoFactory.Create(
                bowlerId: eligibleBowlerId,
                tournamentDate: new DateOnly(2025, 1, 1), tournamentEndDate: new DateOnly(2025, 1, 2),
                statsEligible: true, points: 100),
            BoyProgressionResultDtoFactory.Create(
                bowlerId: ineligibleBowlerId,
                tournamentDate: new DateOnly(2025, 1, 1), tournamentEndDate: new DateOnly(2025, 1, 2),
                statsEligible: false, points: 200),
        };

        var progressions = _calculator.CalculateAllProgressions(results);

        var openSeries = progressions[BowlerOfTheYearCategory.Open.Value];
        openSeries.Count.ShouldBe(1);
        openSeries.ShouldContain(s => s.BowlerId == eligibleBowlerId);
        openSeries.ShouldNotContain(s => s.BowlerId == ineligibleBowlerId);
    }

    // ─── Rookie ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "CalculateAllProgressions should always return empty series for Rookie category")]
    public void CalculateAllProgressions_ShouldAlwaysReturnEmptySeries_ForRookieCategory()
    {
        var results = BoyProgressionResultDtoFactory.Bogus(count: 10, seed: 77);

        var progressions = _calculator.CalculateAllProgressions(results);

        progressions[BowlerOfTheYearCategory.Rookie.Value].ShouldBeEmpty();
    }

    // ─── Side-cut point award ─────────────────────────────────────────────────

    [Fact(DisplayName = "CalculateAllProgressions should award 5 points for side-cut that does not target Open category")]
    public void CalculateAllProgressions_ShouldAward5Points_ForSideCutNotTargetingOpenCategory()
    {
        var bowlerId = BowlerId.New();
        var tournament = TournamentId.New();

        var results = new[]
        {
            BoyProgressionResultDtoFactory.Create(
                bowlerId: bowlerId, tournamentId: tournament,
                tournamentDate: new DateOnly(2025, 1, 1), tournamentEndDate: new DateOnly(2025, 1, 2),
                statsEligible: true, points: 200,
                sideCutId: 1, sideCutName: "Senior"),
        };

        var progressions = _calculator.CalculateAllProgressions(results);

        var openSeries = progressions[BowlerOfTheYearCategory.Open.Value];
        openSeries.Count.ShouldBe(1);
        // Senior side-cut contributes only 5 pts to Open — result.Points (200) is not awarded
        openSeries.Single().Results.Single().CumulativePoints.ShouldBe(5);
    }

    [Fact(DisplayName = "CalculateAllProgressions should award full points for side-cut that targets the race category")]
    public void CalculateAllProgressions_ShouldAwardFullPoints_ForSideCutMatchingRaceCategory()
    {
        var bowlerId = BowlerId.New();
        var tournament = TournamentId.New();
        var endDate = new DateOnly(2025, 1, 1);

        // Bowler aged 55 enters a stats-eligible tournament with a Senior side-cut
        var results = new[]
        {
            BoyProgressionResultDtoFactory.Create(
                bowlerId: bowlerId,
                bowlerDateOfBirth: new DateOnly(1970, 1, 1),
                tournamentId: tournament,
                tournamentDate: endDate, tournamentEndDate: endDate,
                statsEligible: true, points: 200,
                sideCutId: 1, sideCutName: "Senior"),
        };

        var progressions = _calculator.CalculateAllProgressions(results);

        var seniorSeries = progressions[BowlerOfTheYearCategory.Senior.Value];
        seniorSeries.Count.ShouldBe(1);
        // Side-cut matches Senior race — full 200 pts awarded
        seniorSeries.Single().Results.Single().CumulativePoints.ShouldBe(200);
    }

    // ─── Senior race — bowler eligibility ────────────────────────────────────

    [Fact(DisplayName = "Senior race should include bowler who is exactly 50 on tournament end date and exclude one who is 49")]
    public void SeniorRace_ShouldIncludeBowlerExactly50_AndExcludeBowler49_OnTournamentEndDate()
    {
        var seniorBowlerId = BowlerId.New();
        var nonSeniorBowlerId = BowlerId.New();
        var tournament = TournamentId.New();
        var endDate = new DateOnly(2025, 6, 15);

        var results = new[]
        {
            // Birthday is today — turns 50 exactly on end date → eligible
            BoyProgressionResultDtoFactory.Create(
                bowlerId: seniorBowlerId,
                bowlerDateOfBirth: new DateOnly(1975, 6, 15),
                tournamentId: tournament,
                tournamentDate: new DateOnly(2025, 6, 14), tournamentEndDate: endDate,
                statsEligible: true, points: 100),
            // Birthday is tomorrow — still 49 on end date → not eligible
            BoyProgressionResultDtoFactory.Create(
                bowlerId: nonSeniorBowlerId,
                bowlerDateOfBirth: new DateOnly(1975, 6, 16),
                tournamentId: tournament,
                tournamentDate: new DateOnly(2025, 6, 14), tournamentEndDate: endDate,
                statsEligible: true, points: 200),
        };

        var progressions = _calculator.CalculateAllProgressions(results);

        var seniorSeries = progressions[BowlerOfTheYearCategory.Senior.Value];
        seniorSeries.Count.ShouldBe(1);
        seniorSeries.Single().BowlerId.ShouldBe(seniorBowlerId);
    }

    [Fact(DisplayName = "Senior race should count non-stats-eligible results from Senior-format tournaments")]
    public void SeniorRace_ShouldCountNonStatsEligibleResults_FromSeniorFormatTournaments()
    {
        var seniorBowlerId = BowlerId.New();
        var tournament = TournamentId.New();
        var endDate = new DateOnly(2025, 1, 1);

        var results = new[]
        {
            // Tournament is not stats-eligible but is Senior-format — counts for Senior race
            BoyProgressionResultDtoFactory.Create(
                bowlerId: seniorBowlerId,
                bowlerDateOfBirth: new DateOnly(1970, 1, 1),
                tournamentId: tournament,
                tournamentDate: endDate, tournamentEndDate: endDate,
                statsEligible: false, tournamentType: TournamentType.Senior.Value,
                points: 150),
        };

        var progressions = _calculator.CalculateAllProgressions(results);

        var seniorSeries = progressions[BowlerOfTheYearCategory.Senior.Value];
        seniorSeries.Count.ShouldBe(1);
        seniorSeries.Single().Results.Single().CumulativePoints.ShouldBe(150);
    }

    // ─── SuperSenior race — bowler eligibility ───────────────────────────────

    [Fact(DisplayName = "SuperSenior race should include bowler who is exactly 60 on tournament end date and exclude one who is 59")]
    public void SuperSeniorRace_ShouldIncludeBowlerExactly60_AndExcludeBowler59_OnTournamentEndDate()
    {
        var superSeniorBowlerId = BowlerId.New();
        var seniorOnlyBowlerId = BowlerId.New();
        var tournament = TournamentId.New();
        var endDate = new DateOnly(2025, 6, 15);

        var results = new[]
        {
            // Birthday is today — turns 60 exactly on end date → eligible
            BoyProgressionResultDtoFactory.Create(
                bowlerId: superSeniorBowlerId,
                bowlerDateOfBirth: new DateOnly(1965, 6, 15),
                tournamentId: tournament,
                tournamentDate: new DateOnly(2025, 6, 14), tournamentEndDate: endDate,
                statsEligible: true, points: 100),
            // Birthday is tomorrow — still 59 on end date → not eligible
            BoyProgressionResultDtoFactory.Create(
                bowlerId: seniorOnlyBowlerId,
                bowlerDateOfBirth: new DateOnly(1965, 6, 16),
                tournamentId: tournament,
                tournamentDate: new DateOnly(2025, 6, 14), tournamentEndDate: endDate,
                statsEligible: true, points: 200),
        };

        var progressions = _calculator.CalculateAllProgressions(results);

        var superSeniorSeries = progressions[BowlerOfTheYearCategory.SuperSenior.Value];
        superSeniorSeries.Count.ShouldBe(1);
        superSeniorSeries.Single().BowlerId.ShouldBe(superSeniorBowlerId);
    }

    // ─── Woman race — bowler eligibility ─────────────────────────────────────

    [Fact(DisplayName = "Woman race should include only female bowlers and exclude male bowlers")]
    public void WomanRace_ShouldIncludeOnlyFemaleBowlers_AndExcludeMaleBowlers()
    {
        var femaleBowlerId = BowlerId.New();
        var maleBowlerId = BowlerId.New();
        var tournament = TournamentId.New();

        var results = new[]
        {
            BoyProgressionResultDtoFactory.Create(
                bowlerId: femaleBowlerId,
                bowlerGender: Gender.Female.Value,
                tournamentId: tournament,
                tournamentDate: new DateOnly(2025, 1, 1), tournamentEndDate: new DateOnly(2025, 1, 2),
                statsEligible: true, points: 100),
            BoyProgressionResultDtoFactory.Create(
                bowlerId: maleBowlerId,
                bowlerGender: Gender.Male.Value,
                tournamentId: tournament,
                tournamentDate: new DateOnly(2025, 1, 1), tournamentEndDate: new DateOnly(2025, 1, 2),
                statsEligible: true, points: 200),
        };

        var progressions = _calculator.CalculateAllProgressions(results);

        var womanSeries = progressions[BowlerOfTheYearCategory.Woman.Value];
        womanSeries.Count.ShouldBe(1);
        womanSeries.Single().BowlerId.ShouldBe(femaleBowlerId);
    }

    // ─── Youth race — bowler eligibility ─────────────────────────────────────

    [Fact(DisplayName = "Youth race should include bowler who is 17 on tournament end date and exclude one who turns 18 that day")]
    public void YouthRace_ShouldIncludeBowlerAge17_AndExcludeBowlerWhoTurns18_OnTournamentEndDate()
    {
        var youthBowlerId = BowlerId.New();
        var adultBowlerId = BowlerId.New();
        var tournament = TournamentId.New();
        var endDate = new DateOnly(2025, 1, 1);

        var results = new[]
        {
            // Birthday is tomorrow — 17 on end date → youth
            BoyProgressionResultDtoFactory.Create(
                bowlerId: youthBowlerId,
                bowlerDateOfBirth: new DateOnly(2007, 1, 2),
                tournamentId: tournament,
                tournamentDate: endDate, tournamentEndDate: endDate,
                statsEligible: true, points: 100),
            // Birthday is today — turns 18 exactly → no longer youth
            BoyProgressionResultDtoFactory.Create(
                bowlerId: adultBowlerId,
                bowlerDateOfBirth: new DateOnly(2007, 1, 1),
                tournamentId: tournament,
                tournamentDate: endDate, tournamentEndDate: endDate,
                statsEligible: true, points: 200),
        };

        var progressions = _calculator.CalculateAllProgressions(results);

        var youthSeries = progressions[BowlerOfTheYearCategory.Youth.Value];
        youthSeries.Count.ShouldBe(1);
        youthSeries.Single().BowlerId.ShouldBe(youthBowlerId);
    }
}
