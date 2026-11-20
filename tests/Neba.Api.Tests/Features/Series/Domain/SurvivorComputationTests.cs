using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SurvivorComputationTests
{
    private static StageComputation Compute(int count, IReadOnlyCollection<BowlerId> field, params SquadBowlerTotal[] totals)
        => SurvivorComputation.Compute(
            SeriesStageFactory.Create(StageKind.Survivor, count),
            field,
            StageSquadFactFactory.Create(totals: totals));

    private static SquadBowlerTotal Total(BowlerId id, int total, int games = SquadBowlerTotalFactory.ValidGames)
        => SquadBowlerTotalFactory.Create(id, total, games);

    [Fact(DisplayName = "Compute should advance the top N bowlers by total")]
    public void Compute_ShouldAdvanceTopN()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(3);

        // Act
        var result = Compute(2, ids, Total(ids[0], 1600), Total(ids[1], 1800), Total(ids[2], 1700));

        // Assert
        result.Outcomes.ShouldBe([
            new SurvivorOutcomeDraft(ids[0], 1600, false),
            new SurvivorOutcomeDraft(ids[1], 1800, true),
            new SurvivorOutcomeDraft(ids[2], 1700, true)]);
    }

    [Fact(DisplayName = "Compute should advance every bowler tied at the cut")]
    public void Compute_ShouldAdvanceAllTiedAtCut()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(4);

        // Act
        var result = Compute(2, ids, Total(ids[0], 1900), Total(ids[1], 1800), Total(ids[2], 1800), Total(ids[3], 1700));

        // Assert
        result.Outcomes.Where(o => o.Advanced).Select(o => o.BowlerId).ShouldBe([ids[0], ids[1], ids[2]]);
    }

    [Fact(DisplayName = "Compute should score a partial block on the games bowled")]
    public void Compute_ShouldUsePartialBlockTotal()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(2);

        // Act
        var result = Compute(1, ids, Total(ids[0], 900, games: 4), Total(ids[1], 800));

        // Assert
        result.Outcomes.ShouldBe([
            new SurvivorOutcomeDraft(ids[0], 900, true),
            new SurvivorOutcomeDraft(ids[1], 800, false)]);
    }

    [Fact(DisplayName = "Compute should eliminate a no-show and not give the spot to an alternate")]
    public void Compute_ShouldEliminateNoShow()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(3);

        // Act
        var result = Compute(2, ids, Total(ids[1], 1800), Total(ids[2], 1700));

        // Assert
        result.Outcomes.ShouldBe([
            new SurvivorOutcomeDraft(ids[0], null, false),
            new SurvivorOutcomeDraft(ids[1], 1800, true),
            new SurvivorOutcomeDraft(ids[2], 1700, true)]);
    }

    [Fact(DisplayName = "Compute should treat a squad row with zero games as a no-show")]
    public void Compute_ShouldTreatZeroGamesAsNoShow()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(2);

        // Act
        var result = Compute(2, ids, Total(ids[0], 0, games: 0), Total(ids[1], 1500));

        // Assert
        result.Outcomes.ShouldBe([
            new SurvivorOutcomeDraft(ids[0], null, false),
            new SurvivorOutcomeDraft(ids[1], 1500, true)]);
    }

    [Fact(DisplayName = "Compute should advance everyone who bowled when fewer bowled than the count")]
    public void Compute_ShouldAdvanceAllWhoBowled_WhenFewerThanCount()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(3);

        // Act
        var result = Compute(5, ids, Total(ids[0], 1000), Total(ids[1], 1100), Total(ids[2], 1200));

        // Assert
        result.Outcomes.ShouldAllBe(o => o.Advanced);
    }

    [Fact(DisplayName = "Compute should advance nobody when the Stage has no count")]
    public void Compute_ShouldAdvanceNobody_WhenCountIsNotPositive()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(2);
        var stage = SeriesStageFactory.Create(StageKind.Placement);
        var squad = StageSquadFactFactory.Create(totals: [Total(ids[0], 1800), Total(ids[1], 1700)]);

        // Act
        var result = SurvivorComputation.Compute(stage, ids, squad);

        // Assert
        result.Outcomes.Select(o => o.BowlerId).ShouldBe(ids);
        result.Outcomes.ShouldAllBe(o => !o.Advanced);
    }

    [Fact(DisplayName = "Compute should ignore a bowler on the squad who is not in the field")]
    public void Compute_ShouldIgnoreBowlersOutsideField()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(3);

        // Act
        var result = Compute(1, [ids[0], ids[1]], Total(ids[0], 1500), Total(ids[1], 1400), Total(ids[2], 2000));

        // Assert
        result.Outcomes.Select(o => o.BowlerId).ShouldBe([ids[0], ids[1]]);
        result.Outcomes.Single(o => o.Advanced).BowlerId.ShouldBe(ids[0]);
    }

    [Fact(DisplayName = "Compute should return equal output for the same input twice and for reversed input")]
    public void Compute_ShouldBeDeterministic()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(4);
        var totals = new[] { Total(ids[0], 1800), Total(ids[1], 1800), Total(ids[2], 1600), Total(ids[3], 1900) };

        // Act
        var first = Compute(2, ids, totals);
        var second = Compute(2, ids, totals);
        var reversed = Compute(2, [.. ids.Reverse()], [.. totals.Reverse()]);

        // Assert
        second.Outcomes.ShouldBe(first.Outcomes);
        reversed.Outcomes.ShouldBe(first.Outcomes);
    }
}