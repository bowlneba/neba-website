using Neba.Application.Stats.BoyProgression;
using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Stats.BoyProgression;

namespace Neba.Application.Tests.Stats.BoyProgression;

[UnitTest]
[Component("Stats")]
public sealed class BowlerOfTheYearProgressionServiceTests
{
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
            sideCutId: 3);

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
        var t1 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerId, tournamentDate: new DateOnly(2025, 1, 1),  statsEligible: true, points: 50, sideCutId: null);
        var t2 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerId, tournamentDate: new DateOnly(2025, 2, 1),  statsEligible: true, points: 75, sideCutId: null);
        var t3 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerId, tournamentDate: new DateOnly(2025, 3, 1),  statsEligible: true, points: 100, sideCutId: null);

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
        var t3 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerId, tournamentDate: new DateOnly(2025, 3, 1), statsEligible: true, points: 100, sideCutId: null);
        var t1 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerId, tournamentDate: new DateOnly(2025, 1, 1), statsEligible: true, points: 50,  sideCutId: null);
        var t2 = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerId, tournamentDate: new DateOnly(2025, 2, 1), statsEligible: true, points: 75,  sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([t3, t1, t2]);

        var results = progressions[BowlerOfTheYearCategory.Open.Value].Single().Results.ToArray();
        results[0].TournamentDate.ShouldBe(new DateOnly(2025, 1, 1));
        results[0].CumulativePoints.ShouldBe(50);
        results[1].TournamentDate.ShouldBe(new DateOnly(2025, 2, 1));
        results[1].CumulativePoints.ShouldBe(125);
        results[2].TournamentDate.ShouldBe(new DateOnly(2025, 3, 1));
        results[2].CumulativePoints.ShouldBe(225);
    }

    [Fact(DisplayName = "Multiple bowlers: each gets their own series with independent totals")]
    public void ComputeAllProgressions_MultipleBowlers_IndependentSeries()
    {
        var bowlerA = BowlerId.New();
        var bowlerB = BowlerId.New();
        var tA = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerA, statsEligible: true, points: 60, sideCutId: null);
        var tB = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerB, statsEligible: true, points: 90, sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([tA, tB]);

        var openRace = progressions[BowlerOfTheYearCategory.Open.Value];
        openRace.Count.ShouldBe(2);
        openRace.Single(s => s.BowlerId == bowlerA).Results.Single().CumulativePoints.ShouldBe(60);
        openRace.Single(s => s.BowlerId == bowlerB).Results.Single().CumulativePoints.ShouldBe(90);
    }

    [Fact(DisplayName = "Mix of main-cut and side-cut results: each row uses correct point rule, cumulative accurate")]
    public void ComputeAllProgressions_MixedMainAndSideCut_CorrectPointsPerRow()
    {
        var bowlerId = BowlerId.New();
        var main = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerId, tournamentDate: new DateOnly(2025, 1, 1), statsEligible: true, points: 60, sideCutId: null);
        var side = BoyProgressionResultDtoFactory.Create(bowlerId: bowlerId, tournamentDate: new DateOnly(2025, 2, 1), statsEligible: true, points: 80, sideCutId: 2);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([main, side]);

        var results = progressions[BowlerOfTheYearCategory.Open.Value].Single().Results.ToArray();
        results[0].CumulativePoints.ShouldBe(60);
        results[1].CumulativePoints.ShouldBe(65);
    }

    [Fact(DisplayName = "All non-Open races always return empty regardless of input")]
    public void ComputeAllProgressions_NonOpenRaces_AlwaysEmpty()
    {
        var result = BoyProgressionResultDtoFactory.Create(statsEligible: true, points: 100, sideCutId: null);

        var progressions = BowlerOfTheYearProgressionService.ComputeAllProgressions([result]);

        progressions[BowlerOfTheYearCategory.Senior.Value].ShouldBeEmpty();
        progressions[BowlerOfTheYearCategory.SuperSenior.Value].ShouldBeEmpty();
        progressions[BowlerOfTheYearCategory.Woman.Value].ShouldBeEmpty();
        progressions[BowlerOfTheYearCategory.Youth.Value].ShouldBeEmpty();
        progressions[BowlerOfTheYearCategory.Rookie.Value].ShouldBeEmpty();
    }
}
