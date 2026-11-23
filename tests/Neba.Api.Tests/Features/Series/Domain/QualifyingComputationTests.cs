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
        IEnumerable<BowlerId>? members = null,
        IReadOnlyCollection<StageOverride>? overrides = null)
        => QualifyingComputation.Compute(
            stage,
            StageTournamentResultsFactory.Create(results: rows),
            (alreadyQualified ?? []).ToHashSet(),
            (members ?? rows.Select(r => r.BowlerId)).ToHashSet(),
            overrides ?? []);

    private static StageResultRow[] Rows(IReadOnlyList<BowlerId> ids)
        => [.. ids.Select((id, index) => StageResultRowFactory.Create(id, index + 1))];

    private static StageOverride Override(OverrideKind kind, BowlerId bowlerId, DateTimeOffset? createdAt = null)
        => StageOverrideFactory.Create(kind, bowlerId, StageOverrideFactory.ValidReason, StageOverrideFactory.ValidCreatedBy, createdAt);

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

    // ---- Clear overrides ----

    [Fact(DisplayName = "Compute should vacate a filled slot for an applied Clear override")]
    public void Compute_ShouldVacateSlot_WhenClearHitsAFilledSlot()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(3);
        var clear = Override(OverrideKind.Clear, ids[0]);

        // Act
        var result = Compute(stage, Rows(ids), overrides: [clear]);

        // Assert
        result.Entries.Select(e => e.BowlerId).ShouldBe([ids[1]]);
        result.SlotCount.ShouldBe(2);
        result.Vacancies.ShouldBe(1);
        result.OverrideEffects.ShouldBe([new OverrideEffect(clear.Id, true)]);
    }

    [Fact(DisplayName = "Compute should mark a Clear override not applied when the bowler is outside the slots")]
    public void Compute_ShouldNotApplyClear_WhenBowlerIsOutsideTheSlots()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(3);
        var clear = Override(OverrideKind.Clear, ids[2]);

        // Act
        var result = Compute(stage, Rows(ids), overrides: [clear]);

        // Assert
        result.Entries.Select(e => e.BowlerId).ShouldBe([ids[0], ids[1]]);
        result.Vacancies.ShouldBe(0);
        result.OverrideEffects.ShouldBe([new OverrideEffect(clear.Id, false)]);
    }

    [Fact(DisplayName = "Compute should mark a Clear override not applied when the slot was already vacant for a non-member")]
    public void Compute_ShouldNotApplyClear_WhenSlotWasAlreadyVacant()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(3);
        var clear = Override(OverrideKind.Clear, ids[0]);

        // Act
        var result = Compute(stage, Rows(ids), members: [ids[1], ids[2]], overrides: [clear]);

        // Assert
        result.Entries.Select(e => e.BowlerId).ShouldBe([ids[1]]);
        result.Vacancies.ShouldBe(1);
        result.OverrideEffects.ShouldBe([new OverrideEffect(clear.Id, false)]);
    }

    [Fact(DisplayName = "Compute should mark a Clear override not applied when the bowler already qualified in an earlier Stage")]
    public void Compute_ShouldNotApplyClear_WhenBowlerAlreadyQualified()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(3);
        var clear = Override(OverrideKind.Clear, ids[0]);

        // Act
        var result = Compute(stage, Rows(ids), alreadyQualified: [ids[0]], overrides: [clear]);

        // Assert
        result.OverrideEffects.ShouldBe([new OverrideEffect(clear.Id, false)]);
        result.Vacancies.ShouldBe(1);
    }

    // ---- Add overrides ----

    [Fact(DisplayName = "Compute should add a bowler with a result row for an applied Add override, past the slot count")]
    public void Compute_ShouldAddBowler_WhenAddOverrideApplies()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(3);
        var add = Override(OverrideKind.Add, ids[2]);

        // Act
        var result = Compute(stage, Rows(ids), members: [ids[0], ids[1]], overrides: [add]);

        // Assert
        result.Entries.ShouldBe([
            new FieldEntryDraft(ids[0], stage.Id, 1, null, false),
            new FieldEntryDraft(ids[1], stage.Id, 2, null, false),
            new FieldEntryDraft(ids[2], stage.Id, 3, null, true)]);
        result.SlotCount.ShouldBe(2);
        result.OverrideEffects.ShouldBe([new OverrideEffect(add.Id, true)]);
    }

    [Fact(DisplayName = "Compute should not apply an Add override for a bowler with no result row")]
    public void Compute_ShouldNotApplyAdd_WhenBowlerHasNoResultRow()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(3);
        var add = Override(OverrideKind.Add, BowlerId.New());

        // Act
        var result = Compute(stage, Rows(ids), overrides: [add]);

        // Assert
        result.Entries.Count.ShouldBe(2);
        result.OverrideEffects.ShouldBe([new OverrideEffect(add.Id, false)]);
    }

    [Fact(DisplayName = "Compute should not apply an Add override for a bowler already in the field from an earlier Stage")]
    public void Compute_ShouldNotApplyAdd_WhenBowlerAlreadyQualified()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(3);
        var add = Override(OverrideKind.Add, ids[2]);

        // Act
        var result = Compute(stage, Rows(ids), alreadyQualified: [ids[2]], overrides: [add]);

        // Assert
        result.Entries.Select(e => e.BowlerId).ShouldBe([ids[0], ids[1]]);
        result.OverrideEffects.ShouldBe([new OverrideEffect(add.Id, false)]);
    }

    [Fact(DisplayName = "Compute should not apply an Add override for a bowler who already holds a slot in the Stage")]
    public void Compute_ShouldNotApplyAdd_WhenBowlerAlreadyHoldsASlot()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(3);
        var add = Override(OverrideKind.Add, ids[0]);

        // Act
        var result = Compute(stage, Rows(ids), overrides: [add]);

        // Assert
        result.Entries.ShouldBe([
            new FieldEntryDraft(ids[0], stage.Id, 1, null, false),
            new FieldEntryDraft(ids[1], stage.Id, 2, null, false)]);
        result.OverrideEffects.ShouldBe([new OverrideEffect(add.Id, false)]);
    }

    [Fact(DisplayName = "Compute should not let an Add override fill a vacancy and should count only slot entries as filled")]
    public void Compute_ShouldKeepVacancy_WhenAddOverrideApplies()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(3);
        var add = Override(OverrideKind.Add, ids[2]);

        // Act
        var result = Compute(stage, Rows(ids), members: [ids[1], ids[2]], overrides: [add]);

        // Assert
        result.Entries.Count.ShouldBe(2);
        result.Entries.Count(e => e.ViaOverride).ShouldBe(1);
        result.SlotCount.ShouldBe(2);
        result.Vacancies.ShouldBe(1);
    }

    [Fact(DisplayName = "Compute should apply the earlier of two Add overrides for one bowler, by CreatedAt")]
    public void Compute_ShouldApplyEarlierAdd_WhenCreatedAtDiffers()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(3);
        var later = Override(OverrideKind.Add, ids[2], StageOverrideFactory.ValidCreatedAt.AddHours(1));
        var earlier = Override(OverrideKind.Add, ids[2], StageOverrideFactory.ValidCreatedAt);

        // Act
        var result = Compute(stage, Rows(ids), overrides: [later, earlier]);

        // Assert
        result.OverrideEffects.ShouldContain(new OverrideEffect(earlier.Id, true));
        result.OverrideEffects.ShouldContain(new OverrideEffect(later.Id, false));
        result.Entries.Count(e => e.BowlerId == ids[2]).ShouldBe(1);
    }

    [Fact(DisplayName = "Compute should order Add overrides by CreatedAt before it adds their bowlers")]
    public void Compute_ShouldOrderAddsByCreatedAt()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 1);
        var ids = AdvancementTestData.Ids(3);
        var lateAdd = Override(OverrideKind.Add, ids[1], StageOverrideFactory.ValidCreatedAt.AddHours(1));
        var earlyAdd = Override(OverrideKind.Add, ids[2], StageOverrideFactory.ValidCreatedAt);

        // Act
        var result = Compute(stage, Rows(ids), overrides: [lateAdd, earlyAdd]);

        // Assert
        result.Entries.Select(e => e.BowlerId).ShouldBe([ids[0], ids[2], ids[1]]);
    }

    [Fact(DisplayName = "Compute should order Add overrides with the same CreatedAt by Id")]
    public void Compute_ShouldOrderAddsById_WhenCreatedAtIsEqual()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 1);
        var ids = AdvancementTestData.Ids(3);
        var first = Override(OverrideKind.Add, ids[1]);
        var second = Override(OverrideKind.Add, ids[2]);
        var byId = new[] { first, second }.OrderBy(o => o.Id).ToList();

        // Act
        var result = Compute(stage, Rows(ids), overrides: [second, first]);

        // Assert
        result.Entries.Skip(1).Select(e => e.BowlerId).ShouldBe(byId.Select(o => o.BowlerId));
    }

    [Fact(DisplayName = "Compute should apply the lower-Id Add override for one bowler when CreatedAt is equal")]
    public void Compute_ShouldApplyLowerIdAdd_WhenCreatedAtIsEqual()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(3);
        var first = Override(OverrideKind.Add, ids[2]);
        var second = Override(OverrideKind.Add, ids[2]);
        var winner = new[] { first, second }.MinBy(o => o.Id)!;
        var loser = winner == first ? second : first;

        // Act
        var result = Compute(stage, Rows(ids), overrides: [first, second]);

        // Assert
        result.OverrideEffects.ShouldContain(new OverrideEffect(winner.Id, true));
        result.OverrideEffects.ShouldContain(new OverrideEffect(loser.Id, false));
    }

    [Fact(DisplayName = "Compute should report an effect for every override")]
    public void Compute_ShouldReportAnEffectForEveryOverride()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(4);
        var clear = Override(OverrideKind.Clear, ids[0]);
        var add = Override(OverrideKind.Add, ids[3]);

        // Act
        var result = Compute(stage, Rows(ids), overrides: [add, clear]);

        // Assert
        result.OverrideEffects.Count.ShouldBe(2);
        result.OverrideEffects.ShouldContain(new OverrideEffect(clear.Id, true));
        result.OverrideEffects.ShouldContain(new OverrideEffect(add.Id, true));
    }

    [Fact(DisplayName = "Compute should treat an unplaced row (place below 1) as missing the cut")]
    public void Compute_ShouldIgnoreUnplacedRows()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 2);
        var ids = AdvancementTestData.Ids(4);
        var rows = new[]
        {
            StageResultRowFactory.Create(ids[0], 1), StageResultRowFactory.Create(ids[1], 2),
            StageResultRowFactory.Create(ids[2], -1), StageResultRowFactory.Create(ids[3], -1),
        };

        // Act
        var result = Compute(stage, rows);

        // Assert
        result.Entries.Select(e => e.BowlerId).ShouldBe([ids[0], ids[1]]);
        result.SlotCount.ShouldBe(2);
        result.Vacancies.ShouldBe(0);
    }

    [Fact(DisplayName = "Compute should not apply an Add override for an unplaced bowler")]
    public void Compute_ShouldNotAddUnplacedBowler()
    {
        // Arrange
        var stage = SeriesStageFactory.Create(StageKind.Qualifying, 1);
        var ids = AdvancementTestData.Ids(2);
        var rows = new[] { StageResultRowFactory.Create(ids[0], 1), StageResultRowFactory.Create(ids[1], -1) };
        var add = Override(OverrideKind.Add, ids[1]);

        // Act
        var result = Compute(stage, rows, overrides: [add]);

        // Assert
        result.Entries.Select(e => e.BowlerId).ShouldBe([ids[0]]);
        result.OverrideEffects.ShouldContain(new OverrideEffect(add.Id, false));
    }
}