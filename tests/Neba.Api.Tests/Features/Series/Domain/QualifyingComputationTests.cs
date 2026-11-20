using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class QualifyingComputationTests
{
    private static StageComputation Compute(
        SeriesStage stage,
        IReadOnlyList<StageResultRow> rows,
        IEnumerable<BowlerId>? alreadyQualified = null,
        IEnumerable<BowlerId>? members = null)
        => QualifyingComputation.Compute(
            stage,
            StageTournamentResultsFactory.Create(results: rows),
            (alreadyQualified ?? []).ToHashSet(),
            (members ?? rows.Select(r => r.BowlerId)).ToHashSet(),
            []);

    [Fact(DisplayName = "Compute should take the top N places as field entries sourced from the Stage")]
    public void Compute_ShouldTakeTopN_WhenNoTies()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(3);
        var rows = new[] { StageResultRowFactory.Create(ids[0], 1), StageResultRowFactory.Create(ids[1], 2), StageResultRowFactory.Create(ids[2], 3) };

        // Act
        var result = Compute(stage, rows);

        // Assert
        result.Entries.ShouldBe([
            new FieldEntryDraft(ids[0], stage.Id, 1, null, false),
            new FieldEntryDraft(ids[1], stage.Id, 2, null, false)]);
        result.SlotCount.ShouldBe(2);
        result.Vacancies.ShouldBe(0);
        result.OverrideEffects.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Compute should count every bowler tied at place N")]
    public void Compute_ShouldCountAllTiedAtN()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(4);
        var rows = new[]
        {
            StageResultRowFactory.Create(ids[0], 1), StageResultRowFactory.Create(ids[1], 2),
            StageResultRowFactory.Create(ids[2], 2), StageResultRowFactory.Create(ids[3], 4),
        };

        // Act
        var result = Compute(stage, rows);

        // Assert
        result.Entries.Select(e => e.BowlerId).ShouldBe([ids[0], ids[1], ids[2]]);
        result.SlotCount.ShouldBe(3);
        result.Vacancies.ShouldBe(0);
    }

    [Fact(DisplayName = "Compute should count rows in a team event so N teams give N times the team size")]
    public void Compute_ShouldCountRows_WhenTeamEvent()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(6);
        var rows = new[]
        {
            StageResultRowFactory.Create(ids[0], 1), StageResultRowFactory.Create(ids[1], 1),
            StageResultRowFactory.Create(ids[2], 2), StageResultRowFactory.Create(ids[3], 2),
            StageResultRowFactory.Create(ids[4], 3), StageResultRowFactory.Create(ids[5], 3),
        };

        // Act
        var result = Compute(stage, rows);

        // Assert
        result.Entries.Count.ShouldBe(4);
        result.SlotCount.ShouldBe(4);
    }

    [Fact(DisplayName = "Compute should leave a slot vacant for a non-member and not pass it down the finish list")]
    public void Compute_ShouldLeaveVacancy_WhenNotMember()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(3);
        var rows = new[] { StageResultRowFactory.Create(ids[0], 1), StageResultRowFactory.Create(ids[1], 2), StageResultRowFactory.Create(ids[2], 3) };

        // Act
        var result = Compute(stage, rows, members: [ids[1], ids[2]]);

        // Assert
        result.Entries.Select(e => e.BowlerId).ShouldBe([ids[1]]);
        result.SlotCount.ShouldBe(2);
        result.Vacancies.ShouldBe(1);
    }

    [Fact(DisplayName = "Compute should leave a slot vacant for a bowler already in the field")]
    public void Compute_ShouldLeaveVacancy_WhenAlreadyQualified()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(3);
        var rows = new[] { StageResultRowFactory.Create(ids[0], 1), StageResultRowFactory.Create(ids[1], 2), StageResultRowFactory.Create(ids[2], 3) };

        // Act
        var result = Compute(stage, rows, alreadyQualified: [ids[0]]);

        // Assert
        result.Entries.Select(e => e.BowlerId).ShouldBe([ids[1]]);
        result.Vacancies.ShouldBe(1);
    }

    [Fact(DisplayName = "Compute should return no entries when the tournament has no results")]
    public void Compute_ShouldReturnNothing_WhenNoResults()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 3);

        // Act
        var result = Compute(stage, []);

        // Assert
        result.Entries.ShouldBeEmpty();
        result.SlotCount.ShouldBe(0);
        result.Vacancies.ShouldBe(0);
    }

    [Fact(DisplayName = "Compute should order entries by place then BowlerId whatever the input order")]
    public void Compute_ShouldOrderByPlaceThenBowlerId()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(3);
        var rows = new[] { StageResultRowFactory.Create(ids[2], 2), StageResultRowFactory.Create(ids[1], 1), StageResultRowFactory.Create(ids[0], 2) };

        // Act
        var result = Compute(stage, rows);

        // Assert
        result.Entries.Select(e => e.BowlerId).ShouldBe([ids[1], ids[0], ids[2]]);
    }

    [Fact(DisplayName = "Compute should return equal output for the same input twice and for reversed result rows")]
    public void Compute_ShouldBeDeterministic()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 3);
        var ids = AdvancementTestData.Ids(6);
        var rows = new[]
        {
            StageResultRowFactory.Create(ids[0], 1), StageResultRowFactory.Create(ids[1], 2), StageResultRowFactory.Create(ids[2], 3),
            StageResultRowFactory.Create(ids[3], 3), StageResultRowFactory.Create(ids[4], 5), StageResultRowFactory.Create(ids[5], 6),
        };

        // Act
        var first = Compute(stage, rows, members: ids.Skip(1));
        var second = Compute(stage, rows, members: ids.Skip(1));
        var reversed = Compute(stage, [.. rows.Reverse()], members: ids.Skip(1).Reverse());

        // Assert
        second.Entries.ShouldBe(first.Entries);
        reversed.Entries.ShouldBe(first.Entries);
        reversed.SlotCount.ShouldBe(first.SlotCount);
        reversed.Vacancies.ShouldBe(first.Vacancies);
    }
}