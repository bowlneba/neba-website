using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class PlacementComputationTests
{
    private static StageComputation Compute(
        IReadOnlyCollection<BowlerId> field,
        IReadOnlyList<StageResultRow> rows,
        IReadOnlyDictionary<BowlerId, BowlerSortName>? names = null,
        int? finalistCount = null)
        => PlacementComputation.Compute(
            field,
            StageTournamentResultsFactory.Create(results: rows),
            names ?? new Dictionary<BowlerId, BowlerSortName>(),
            finalistCount);

    private static StageResultRow Row(BowlerId id, int place) => StageResultRowFactory.Create(id, place);

    [Fact(DisplayName = "Compute should rank bowlers who bowled by place with unique positions")]
    public void Compute_ShouldRankByPlace()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(3);

        // Act
        var result = Compute(ids, [Row(ids[0], 3), Row(ids[1], 1), Row(ids[2], 2)]);

        // Assert
        result.Positions.ShouldBe([
            new PositionDraft(ids[1], 1, true, null),
            new PositionDraft(ids[2], 2, true, null),
            new PositionDraft(ids[0], 3, true, null)]);
    }

    [Fact(DisplayName = "Compute should break tied places by BowlerId")]
    public void Compute_ShouldBreakTiesByBowlerId()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(3);

        // Act
        var result = Compute([ids[2], ids[0], ids[1]], [Row(ids[2], 1), Row(ids[1], 1), Row(ids[0], 1)]);

        // Assert
        result.Positions.Select(p => p.BowlerId).ShouldBe([ids[0], ids[1], ids[2]]);
        result.Positions.Select(p => p.Position).ShouldBe([1, 2, 3]);
    }

    [Fact(DisplayName = "Compute should rank no-shows after bowlers who bowled, by last then first name")]
    public void Compute_ShouldOrderNoShowsByName()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(5);
        var names = new Dictionary<BowlerId, BowlerSortName>
        {
            [ids[1]] = BowlerSortNameFactory.Create("smith", "Zed"),
            [ids[2]] = BowlerSortNameFactory.Create("Smith", "Amy"),
            [ids[3]] = BowlerSortNameFactory.Create("Adams", "Bob"),
            [ids[4]] = BowlerSortNameFactory.Create("Jones", "Cy"),
        };

        // Act
        var result = Compute(ids, [Row(ids[0], 9)], names);

        // Assert
        result.Positions.Select(p => p.BowlerId).ShouldBe([ids[0], ids[3], ids[4], ids[2], ids[1]]);
        result.Positions.Select(p => p.Bowled).ShouldBe([true, false, false, false, false]);
        result.Positions.Select(p => p.Position).ShouldBe([1, 2, 3, 4, 5]);
    }

    [Fact(DisplayName = "Compute should break equal no-show names by BowlerId")]
    public void Compute_ShouldBreakNoShowNameTiesByBowlerId()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(2);
        var sameName = BowlerSortNameFactory.Create();
        var names = new Dictionary<BowlerId, BowlerSortName> { [ids[0]] = sameName, [ids[1]] = sameName };

        // Act
        var result = Compute([ids[1], ids[0]], [], names);

        // Assert
        result.Positions.Select(p => p.BowlerId).ShouldBe([ids[0], ids[1]]);
        result.Positions.ShouldAllBe(p => !p.Bowled);
    }

    [Fact(DisplayName = "Compute should ignore result rows for bowlers outside the field")]
    public void Compute_ShouldIgnoreBowlersOutsideField()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(2);

        // Act
        var result = Compute([ids[1]], [Row(ids[0], 1), Row(ids[1], 2)]);

        // Assert
        result.Positions.ShouldBe([new PositionDraft(ids[1], 1, true, null)]);
    }

    [Fact(DisplayName = "Compute should mark the first finalist-count positions as finalists")]
    public void Compute_ShouldFlagFinalists_WhenFinaleHasCount()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(4);

        // Act
        var result = Compute(ids, [Row(ids[0], 1), Row(ids[1], 2), Row(ids[2], 3)], finalistCount: 2);

        // Assert
        result.Positions.Select(p => p.IsFinalist).ShouldBe([true, true, false, false]);
    }

    [Fact(DisplayName = "Compute should flag every position a finalist when the count exceeds the field")]
    public void Compute_ShouldFlagAll_WhenCountExceedsField()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(2);

        // Act
        var result = Compute(ids, [Row(ids[0], 1), Row(ids[1], 2)], finalistCount: 10);

        // Assert
        result.Positions.ShouldAllBe(p => p.IsFinalist == true);
    }

    [Fact(DisplayName = "Compute should leave the finalist flag null when there is no Finale")]
    public void Compute_ShouldLeaveFinalistNull_WhenNoFinale()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(2);

        // Act
        var result = Compute(ids, [Row(ids[0], 1), Row(ids[1], 2)], finalistCount: null);

        // Assert
        result.Positions.ShouldAllBe(p => p.IsFinalist == null);
    }

    [Fact(DisplayName = "Compute should return equal output for the same input twice and for reversed input")]
    public void Compute_ShouldBeDeterministic()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(6);
        var names = new Dictionary<BowlerId, BowlerSortName>
        {
            [ids[4]] = BowlerSortNameFactory.Create("Young", "Al"),
            [ids[5]] = BowlerSortNameFactory.Create("Adams", "Al"),
        };
        IReadOnlyList<StageResultRow> rows = [Row(ids[0], 2), Row(ids[1], 2), Row(ids[2], 1), Row(ids[3], 3)];

        // Act
        var first = Compute(ids, rows, names, 3);
        var second = Compute(ids, rows, names, 3);
        var reversed = Compute([.. ids.Reverse()], [.. rows.Reverse()], names, 3);

        // Assert
        second.Positions.ShouldBe(first.Positions);
        reversed.Positions.ShouldBe(first.Positions);
    }
}