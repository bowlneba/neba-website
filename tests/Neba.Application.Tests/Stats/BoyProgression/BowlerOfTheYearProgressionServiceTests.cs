using Neba.Application.Stats.BoyProgression;
using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;
using Neba.Domain.Tournaments;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Stats.BoyProgression;

namespace Neba.Application.Tests.Stats.BoyProgression;

[UnitTest]
[Component("Stats")]
public sealed class BowlerOfTheYearProgressionServiceTests
{
    // ── Open race ────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Empty input returns dictionary with all six keys and empty Open race")]
    public void ComputeAllProgressions_EmptyInput_ReturnsAllKeysEmpty()
    {
        var result = BowlerOfTheYearProgressionService.ComputeAllProgressions([]);

        result.Keys.ShouldBe(
            [BowlerOfTheYearCategory.Open.Value, BowlerOfTheYearCategory.Senior.Value, BowlerOfTheYearCategory.SuperSenior.Value,
             BowlerOfTheYearCategory.Woman.Value, BowlerOfTheYearCategory.Youth.Value, BowlerOfTheYearCategory.Rookie.Value],
            ignoreOrder: true);
        result[BowlerOfTheYearCategory.Open.Value].ShouldBeEmpty();
    }

    [Fact(DisplayName = "Single bowler, single main-cut result, stat-eligible: cumulative points equals listed points")]
    public void ComputeAllProgressions_SingleMainCutStatEligible_UsesListedPoints()
    {
        var bowlerId = BowlerId.New();
        var bowlerName = NameFactory.Create();
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerId: bowlerId,
            bowlerName: bowlerName,
            statsEligible: true,
            points: 120,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        var openRace = progressions[BowlerOfTheYearCategory.Open.Value];
        openRace.Count.ShouldBe(1);
        var series = openRace.Single();
        series.BowlerId.ShouldBe(bowlerId);
        series.Results.Single().CumulativePoints.ShouldBe(120);
    }

    [Fact(DisplayName = "Single bowler, single side-cut result, stat-eligible: cumulative points equals 5")]
    public void ComputeAllProgressions_SingleSideCutStatEligible_Uses5Points()
    {
        var result = BoyProgressionResultDtoFactory.Create(
            statsEligible: true,
            points: 80,
            sideCutId: 3,
            sideCutName: "Senior");

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        var series = progressions[BowlerOfTheYearCategory.Open.Value].Single();
        series.Results.Single().CumulativePoints.ShouldBe(5);
    }

    [Fact(DisplayName = "Non-stat-eligible tournament: bowler absent from Open race")]
    public void ComputeAllProgressions_NonStatEligible_ExcludedFromOpen()
    {
        var result = BoyProgressionResultDtoFactory.Create(
            statsEligible: false,
            points: 100,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Open.Value].ShouldBeEmpty();
    }

    [Fact(DisplayName = "Single bowler, three tournaments in date order: cumulative total is correct running sum")]
    public void ComputeAllProgressions_ThreeTournaments_CumulativeCorrect()
    {
        var bowlerId = BowlerId.New();
        var t1 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerId, tournamentDate: new DateOnly(2025, 1, 1), tournamentEndDate: new DateOnly(2025, 1, 2), statsEligible: true, points: 50, sideCutId: null);
        var t2 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerId, tournamentDate: new DateOnly(2025, 2, 1), tournamentEndDate: new DateOnly(2025, 2, 2), statsEligible: true, points: 75, sideCutId: null);
        var t3 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerId, tournamentDate: new DateOnly(2025, 3, 1), tournamentEndDate: new DateOnly(2025, 3, 2), statsEligible: true, points: 100, sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([t1, t2, t3]);

        var series = progressions[BowlerOfTheYearCategory.Open.Value].Single();
        var results = series.Results.ToArray();
        results[0].CumulativePoints.ShouldBe(50);
        results[1].CumulativePoints.ShouldBe(125);
        results[2].CumulativePoints.ShouldBe(225);
    }

    [Fact(DisplayName = "Tournaments arriving out of date order: results ordered by date, cumulative correct")]
    public void ComputeAllProgressions_OutOfOrderInput_OrderedByDate()
    {
        var bowlerId = BowlerId.New();
        var t3 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerId, tournamentDate: new DateOnly(2025, 3, 1), tournamentEndDate: new DateOnly(2025, 3, 2), statsEligible: true, points: 100, sideCutId: null);
        var t1 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerId, tournamentDate: new DateOnly(2025, 1, 1), tournamentEndDate: new DateOnly(2025, 1, 2), statsEligible: true, points: 50, sideCutId: null);
        var t2 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerId, tournamentDate: new DateOnly(2025, 2, 1), tournamentEndDate: new DateOnly(2025, 2, 2), statsEligible: true, points: 75, sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([t3, t1, t2]);

        var results = progressions[BowlerOfTheYearCategory.Open.Value].Single().Results.ToArray();
        results[0].TournamentDate.ShouldBe(new DateOnly(2025, 1, 1));
        results[0].CumulativePoints.ShouldBe(50);
        results[1].TournamentDate.ShouldBe(new DateOnly(2025, 2, 1));
        results[1].CumulativePoints.ShouldBe(125);
        results[2].TournamentDate.ShouldBe(new DateOnly(2025, 3, 1));
        results[2].CumulativePoints.ShouldBe(225);
    }

    [Fact(DisplayName = "Multiple bowlers: series are normalized to the same tournament list, each reaches their own total")]
    public void ComputeAllProgressions_MultipleBowlers_NormalizedToSameTournamentList()
    {
        var bowlerA = BowlerId.New();
        var bowlerB = BowlerId.New();
        var tA = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerA, statsEligible: true, points: 60, sideCutId: null);
        var tB = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerB, statsEligible: true, points: 90, sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([tA, tB]);

        var openRace = progressions[BowlerOfTheYearCategory.Open.Value];
        openRace.Count.ShouldBe(2);
        // Both series span all tournaments in the season (2 here).
        openRace.Single(s => s.BowlerId == bowlerA).Results.Count.ShouldBe(2);
        openRace.Single(s => s.BowlerId == bowlerB).Results.Count.ShouldBe(2);
        // Final cumulative for each bowler equals their own total.
        openRace.Single(s => s.BowlerId == bowlerA).Results.Last().CumulativePoints.ShouldBe(60);
        openRace.Single(s => s.BowlerId == bowlerB).Results.Last().CumulativePoints.ShouldBe(90);
    }

    [Fact(DisplayName = "Bowler who skipped a tournament shows flat line at that position")]
    public void ComputeAllProgressions_BowlerMissedTournament_FlatLineAtMissedPosition()
    {
        var bowlerA = BowlerId.New();
        var bowlerB = BowlerId.New();
        var tournamentA = TournamentId.New();
        var tournamentB = TournamentId.New();
        var tournamentC = TournamentId.New();
        // BowlerA competed in A and C but not B; BowlerB competed in all three.
        var rA1 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerA, tournamentId: tournamentA, tournamentDate: new DateOnly(2025, 1, 1), statsEligible: true, points: 50, sideCutId: null);
        var rA2 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerA, tournamentId: tournamentC, tournamentDate: new DateOnly(2025, 3, 1), statsEligible: true, points: 75, sideCutId: null);
        var rB1 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerB, tournamentId: tournamentA, tournamentDate: new DateOnly(2025, 1, 1), statsEligible: true, points: 40, sideCutId: null);
        var rB2 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerB, tournamentId: tournamentB, tournamentDate: new DateOnly(2025, 2, 1), statsEligible: true, points: 60, sideCutId: null);
        var rB3 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerB, tournamentId: tournamentC, tournamentDate: new DateOnly(2025, 3, 1), statsEligible: true, points: 80, sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([rA1, rA2, rB1, rB2, rB3]);

        var seriesA = progressions[BowlerOfTheYearCategory.Open.Value].Single(s => s.BowlerId == bowlerA).Results.ToArray();
        seriesA.Length.ShouldBe(3);
        seriesA[0].CumulativePoints.ShouldBe(50);  // Tournament A
        seriesA[1].CumulativePoints.ShouldBe(50);  // Tournament B — flat, bowler skipped
        seriesA[2].CumulativePoints.ShouldBe(125); // Tournament C
    }

    [Fact(DisplayName = "Mix of main-cut and side-cut results: each row uses correct point rule, cumulative accurate")]
    public void ComputeAllProgressions_MixedMainAndSideCut_CorrectPointsPerRow()
    {
        var bowlerId = BowlerId.New();
        var main = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerId, tournamentDate: new DateOnly(2025, 1, 1), statsEligible: true, points: 60, sideCutId: null);
        var side = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerId, tournamentDate: new DateOnly(2025, 2, 1), statsEligible: true, points: 80, sideCutId: 2, sideCutName: "Senior");

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([main, side]);

        var results = progressions[BowlerOfTheYearCategory.Open.Value].Single().Results.ToArray();
        results[0].CumulativePoints.ShouldBe(60);
        results[1].CumulativePoints.ShouldBe(65); // 5 for the side cut
    }

    // ── Rookie — still deferred ───────────────────────────────────────────────

    [Fact(DisplayName = "Rookie race always returns empty regardless of input (membership data not yet available)")]
    public void ComputeAllProgressions_RookieRace_AlwaysEmpty()
    {
        var result = BoyProgressionResultDtoFactory.Create(statsEligible: true, points: 100, sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Rookie.Value].ShouldBeEmpty();
    }

    // ── Woman race ────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Woman race: female bowler in stat-eligible tournament is included with listed points")]
    public void ComputeAllProgressions_WomanRace_FemaleInStatEligible_Included()
    {
        var bowlerId = BowlerId.New();
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerId: bowlerId,
            bowlerGender: Gender.Female,
            statsEligible: true,
            points: 75,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        var series = progressions[BowlerOfTheYearCategory.Woman.Value].Single();
        series.BowlerId.ShouldBe(bowlerId);
        series.Results.Single().CumulativePoints.ShouldBe(75);
    }

    [Fact(DisplayName = "Woman race: male bowler is excluded")]
    public void ComputeAllProgressions_WomanRace_MaleBowler_Excluded()
    {
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerGender: Gender.Male,
            statsEligible: true,
            points: 75,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Woman.Value].ShouldBeEmpty();
    }

    [Fact(DisplayName = "Woman race: bowler with no gender data is excluded")]
    public void ComputeAllProgressions_WomanRace_NullGender_Excluded()
    {
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerGender: null,
            statsEligible: true,
            points: 75,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Woman.Value].ShouldBeEmpty();
    }

    [Fact(DisplayName = "Woman race: Women-type tournament (not stat-eligible) includes female bowler")]
    public void ComputeAllProgressions_WomanRace_WomenTypeTournament_IncludesFemale()
    {
        var bowlerId = BowlerId.New();
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerId: bowlerId,
            bowlerGender: Gender.Female,
            statsEligible: false,
            tournamentType: TournamentType.Women,
            points: 60,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Woman.Value].Single().BowlerId.ShouldBe(bowlerId);
    }

    [Fact(DisplayName = "Woman race: SeniorAndWomen-type tournament (not stat-eligible) includes female bowler")]
    public void ComputeAllProgressions_WomanRace_SeniorAndWomenTypeTournament_IncludesFemale()
    {
        var bowlerId = BowlerId.New();
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerId: bowlerId,
            bowlerGender: Gender.Female,
            statsEligible: false,
            tournamentType: TournamentType.SeniorAndWomen,
            points: 60,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Woman.Value].Single().BowlerId.ShouldBe(bowlerId);
    }

    [Fact(DisplayName = "Woman race: Women-type tournament excludes male bowler even though tournament is eligible")]
    public void ComputeAllProgressions_WomanRace_WomenTypeTournament_ExcludesMale()
    {
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerGender: Gender.Male,
            statsEligible: false,
            tournamentType: TournamentType.Women,
            points: 60,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Woman.Value].ShouldBeEmpty();
    }

    [Fact(DisplayName = "Woman race: side cut targeting Woman race gives listed points to female bowler")]
    public void ComputeAllProgressions_WomanRace_WomenSideCut_GivesListedPoints()
    {
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerGender: Gender.Female,
            statsEligible: true,
            points: 40,
            sideCutId: 5,
            sideCutName: "Women");

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Woman.Value].Single().Results.Single().CumulativePoints.ShouldBe(40);
    }

    // ── Senior race ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "Senior race: bowler aged exactly 50 at tournament end date is included")]
    public void ComputeAllProgressions_SeniorRace_BowlerTurns50OnEndDate_Included()
    {
        var endDate = new DateOnly(2025, 6, 15);
        var bowlerId = BowlerId.New();
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerId: bowlerId,
            bowlerDateOfBirth: new DateOnly(1975, 6, 15),
            statsEligible: true,
            tournamentEndDate: endDate,
            points: 90,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Senior.Value].Single().BowlerId.ShouldBe(bowlerId);
    }

    [Fact(DisplayName = "Senior race: bowler aged 49 at tournament end date is excluded")]
    public void ComputeAllProgressions_SeniorRace_Bowler49OnEndDate_Excluded()
    {
        var endDate = new DateOnly(2025, 6, 14);
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerDateOfBirth: new DateOnly(1975, 6, 15),
            statsEligible: true,
            tournamentEndDate: endDate,
            points: 90,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Senior.Value].ShouldBeEmpty();
    }

    [Fact(DisplayName = "Senior race: bowler with no date of birth is excluded")]
    public void ComputeAllProgressions_SeniorRace_NullDateOfBirth_Excluded()
    {
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerDateOfBirth: null,
            statsEligible: true,
            points: 90,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Senior.Value].ShouldBeEmpty();
    }

    [Fact(DisplayName = "Senior race: Senior-type tournament (not stat-eligible) includes eligible bowler")]
    public void ComputeAllProgressions_SeniorRace_SeniorTypeTournament_IncludesEligibleBowler()
    {
        var bowlerId = BowlerId.New();
        var endDate = new DateOnly(2025, 6, 15);
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerId: bowlerId,
            bowlerDateOfBirth: new DateOnly(1970, 1, 1),
            statsEligible: false,
            tournamentType: TournamentType.Senior,
            tournamentEndDate: endDate,
            points: 50,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Senior.Value].Single().BowlerId.ShouldBe(bowlerId);
    }

    [Fact(DisplayName = "Senior race: Senior-type tournament does not count toward Open race")]
    public void ComputeAllProgressions_SeniorTypeTournament_NotStatEligible_ExcludedFromOpen()
    {
        var result = BoyProgressionResultDtoFactory.Create(
            statsEligible: false,
            tournamentType: TournamentType.Senior,
            points: 50,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Open.Value].ShouldBeEmpty();
    }

    [Fact(DisplayName = "Senior race: side cut targeting Senior race gives listed points")]
    public void ComputeAllProgressions_SeniorRace_SeniorSideCut_GivesListedPoints()
    {
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerDateOfBirth: new DateOnly(1970, 1, 1),
            statsEligible: true,
            tournamentEndDate: new DateOnly(2025, 6, 15),
            points: 30,
            sideCutId: 2,
            sideCutName: "Senior");

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Senior.Value].Single().Results.Single().CumulativePoints.ShouldBe(30);
    }

    [Fact(DisplayName = "Senior race: side cut targeting another race gives 5 points")]
    public void ComputeAllProgressions_SeniorRace_NonSeniorSideCut_Gives5Points()
    {
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerDateOfBirth: new DateOnly(1970, 1, 1),
            statsEligible: true,
            tournamentEndDate: new DateOnly(2025, 6, 15),
            points: 30,
            sideCutId: 3,
            sideCutName: "Super Senior");

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Senior.Value].Single().Results.Single().CumulativePoints.ShouldBe(5);
    }

    // ── SuperSenior race ──────────────────────────────────────────────────────

    [Fact(DisplayName = "SuperSenior race: bowler aged exactly 60 at tournament end date is included")]
    public void ComputeAllProgressions_SuperSeniorRace_BowlerTurns60OnEndDate_Included()
    {
        var endDate = new DateOnly(2025, 3, 20);
        var bowlerId = BowlerId.New();
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerId: bowlerId,
            bowlerDateOfBirth: new DateOnly(1965, 3, 20),
            statsEligible: true,
            tournamentEndDate: endDate,
            points: 55,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.SuperSenior.Value].Single().BowlerId.ShouldBe(bowlerId);
    }

    [Fact(DisplayName = "SuperSenior race: bowler aged 50-59 is included in Senior but excluded from SuperSenior")]
    public void ComputeAllProgressions_SuperSeniorRace_Bowler55_InSeniorNotSuperSenior()
    {
        var endDate = new DateOnly(2025, 6, 15);
        var bowlerId = BowlerId.New();
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerId: bowlerId,
            bowlerDateOfBirth: new DateOnly(1970, 1, 1), // age 55 in 2025
            statsEligible: true,
            tournamentEndDate: endDate,
            points: 80,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Senior.Value].Single().BowlerId.ShouldBe(bowlerId);
        progressions[BowlerOfTheYearCategory.SuperSenior.Value].ShouldBeEmpty();
    }

    [Fact(DisplayName = "SuperSenior race: Super Senior side cut gives listed points")]
    public void ComputeAllProgressions_SuperSeniorRace_SuperSeniorSideCut_GivesListedPoints()
    {
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerDateOfBirth: new DateOnly(1960, 1, 1),
            statsEligible: true,
            tournamentEndDate: new DateOnly(2025, 6, 15),
            points: 40,
            sideCutId: 4,
            sideCutName: "Super Senior");

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.SuperSenior.Value].Single().Results.Single().CumulativePoints.ShouldBe(40);
    }

    // ── Youth race ────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Youth race: bowler aged 17 at tournament end date is included")]
    public void ComputeAllProgressions_YouthRace_Bowler17OnEndDate_Included()
    {
        var endDate = new DateOnly(2025, 8, 10);
        var bowlerId = BowlerId.New();
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerId: bowlerId,
            bowlerDateOfBirth: new DateOnly(2007, 8, 11), // turns 18 the day after the tournament ends
            statsEligible: true,
            tournamentEndDate: endDate,
            points: 35,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Youth.Value].Single().BowlerId.ShouldBe(bowlerId);
    }

    [Fact(DisplayName = "Youth race: bowler who turns 18 on tournament end date is excluded")]
    public void ComputeAllProgressions_YouthRace_BowlerTurns18OnEndDate_Excluded()
    {
        var endDate = new DateOnly(2025, 8, 10);
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerDateOfBirth: new DateOnly(2007, 8, 10), // turns 18 on end date
            statsEligible: true,
            tournamentEndDate: endDate,
            points: 35,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Youth.Value].ShouldBeEmpty();
    }

    [Fact(DisplayName = "Youth race: non-stat-eligible tournament is excluded (Youth follows Open eligibility rules)")]
    public void ComputeAllProgressions_YouthRace_NonStatEligibleTournament_Excluded()
    {
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerDateOfBirth: new DateOnly(2010, 1, 1),
            statsEligible: false,
            tournamentType: TournamentType.Youth,
            tournamentEndDate: new DateOnly(2025, 6, 15),
            points: 35,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Youth.Value].ShouldBeEmpty();
    }

    // ── Age evaluated against EndDate, not StartDate ──────────────────────────

    [Fact(DisplayName = "Senior eligibility uses tournament end date: bowler who turns 50 on day 2 of a 2-day tournament is eligible")]
    public void ComputeAllProgressions_SeniorRace_BowlerTurns50OnDay2_EligibleForTournament()
    {
        // Two-day tournament: Jan 14–15. Bowler turns 50 on Jan 15 (end date).
        var bowlerId = BowlerId.New();
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerId: bowlerId,
            bowlerDateOfBirth: new DateOnly(1975, 1, 15),
            statsEligible: true,
            tournamentDate: new DateOnly(2025, 1, 14),   // start
            tournamentEndDate: new DateOnly(2025, 1, 15), // end — birthday
            points: 70,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Senior.Value].Single().BowlerId.ShouldBe(bowlerId);
    }

    [Fact(DisplayName = "Senior eligibility uses tournament end date: bowler who turned 50 the day after the tournament ends is not eligible")]
    public void ComputeAllProgressions_SeniorRace_BowlerTurns50DayAfterTournament_NotEligible()
    {
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerDateOfBirth: new DateOnly(1975, 1, 16),
            statsEligible: true,
            tournamentDate: new DateOnly(2025, 1, 14),
            tournamentEndDate: new DateOnly(2025, 1, 15),
            points: 70,
            sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Senior.Value].ShouldBeEmpty();
    }

    // ── Concrete example from plan doc ────────────────────────────────────────

    [Fact(DisplayName = "Plan doc example: Senior tournament, SuperSenior side cut — Senior gets 5, SuperSenior gets listed 40")]
    public void ComputeAllProgressions_PlanDocExample_SeniorTournamentSuperSeniorSideCut()
    {
        // Senior tournament (not stat-eligible), SuperSenior bowler (age 65) in the SuperSenior side cut, listed 40 pts.
        var seniorBowlerId = BowlerId.New();
        var endDate = new DateOnly(2025, 6, 15);
        var result = BoyProgressionResultDtoFactory.Create(
            bowlerId: seniorBowlerId,
            bowlerDateOfBirth: new DateOnly(1960, 1, 1), // age 65
            statsEligible: false,
            tournamentType: TournamentType.Senior,
            tournamentEndDate: endDate,
            points: 40,
            sideCutId: 4,
            sideCutName: "Super Senior");

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        // Open: not stat-eligible → excluded
        progressions[BowlerOfTheYearCategory.Open.Value].ShouldBeEmpty();
        // Senior: Senior tournament is eligible, bowler is ≥50, side cut ≠ Senior → 5 pts
        progressions[BowlerOfTheYearCategory.Senior.Value].Single().Results.Single().CumulativePoints.ShouldBe(5);
        // SuperSenior: Senior tournament is eligible, bowler is ≥60, side cut == SuperSenior → listed 40
        progressions[BowlerOfTheYearCategory.SuperSenior.Value].Single().Results.Single().CumulativePoints.ShouldBe(40);
    }
}
