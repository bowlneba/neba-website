using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.TestFactory.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesAdvancementResponseFactoryTests
{
    [Fact(DisplayName = "SeriesAdvancementResponseFactory Create should return the constants, one Stage and one position with no arguments")]
    public void SeriesAdvancementResponseFactory_Create_ShouldReturnDefaults()
    {
        // Arrange & Act
        var response = SeriesAdvancementResponseFactory.Create();

        // Assert
        response.Version.ShouldBe(SeriesAdvancementResponseFactory.ValidVersion);
        response.TargetField.ShouldBe(SeriesAdvancementResponseFactory.ValidTargetField);
        response.BackfillAsOf.ShouldBeNull();
        response.Stages.Count.ShouldBe(1);
        response.Positions.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "SeriesAdvancementResponseFactory Create should use the given values and empty lists")]
    public void SeriesAdvancementResponseFactory_Create_ShouldUseGivenValues()
    {
        // Arrange
        var backfillAsOf = new DateOnly(2025, 2, 3);

        // Act
        var response = SeriesAdvancementResponseFactory.Create(
            version: 7,
            backfillAsOf: backfillAsOf,
            targetField: 12,
            stages: [],
            positions: []);

        // Assert
        response.Version.ShouldBe(7);
        response.BackfillAsOf.ShouldBe(backfillAsOf);
        response.TargetField.ShouldBe(12);
        response.Stages.ShouldBeEmpty();
        response.Positions.ShouldBeEmpty();
    }

    [Theory(DisplayName = "StageAdvancementResponseFactory Create should give each kind its own default output")]
    [InlineData("Qualifying", true, false, 1, 0)]
    [InlineData("Survivor", false, true, 0, 1)]
    [InlineData("Placement", false, false, 0, 0)]
    public void StageAdvancementResponseFactory_Create_ShouldDefaultByKind(
        string kind, bool hasSlots, bool hasSquad, int entries, int outcomes)
    {
        // Arrange & Act
        var stage = StageAdvancementResponseFactory.Create(kind: kind);

        // Assert
        stage.Kind.ShouldBe(kind);
        stage.Status.ShouldBe(StageAdvancementResponseFactory.ValidStatus);
        stage.TournamentStatus.ShouldBe(StageAdvancementResponseFactory.ValidTournamentStatus);
        stage.StageId.ShouldBe(StageAdvancementResponseFactory.ValidStageId);
        stage.TournamentId.ShouldBe(StageAdvancementResponseFactory.ValidTournamentId);
        stage.Order.ShouldBe(StageAdvancementResponseFactory.ValidOrder);
        stage.IsStale.ShouldBeFalse();
        stage.NeedsSquad.ShouldBeFalse();
        stage.SlotCount.HasValue.ShouldBe(hasSlots);
        stage.VacantSlotCount.HasValue.ShouldBe(hasSlots);
        (stage.ResolvedSquadId is not null).ShouldBe(hasSquad);
        stage.Entries.Count.ShouldBe(entries);
        stage.SurvivorOutcomes.Count.ShouldBe(outcomes);
        stage.Overrides.ShouldBeEmpty();
    }

    [Fact(DisplayName = "StageAdvancementResponseFactory Create should use the given values")]
    public void StageAdvancementResponseFactory_Create_ShouldUseGivenValues()
    {
        // Arrange
        var computedAt = new DateTimeOffset(2025, 4, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var stage = StageAdvancementResponseFactory.Create(
            stageId: "stage",
            order: 3,
            kind: "Placement",
            status: "Official",
            isStale: true,
            needsSquad: true,
            computedAt: computedAt,
            slotCount: 5,
            tournamentId: "tournament",
            tournamentStatus: "Completed",
            entries: [],
            survivorOutcomes: []);

        // Assert
        stage.StageId.ShouldBe("stage");
        stage.Order.ShouldBe(3);
        stage.Kind.ShouldBe("Placement");
        stage.Status.ShouldBe("Official");
        stage.IsStale.ShouldBeTrue();
        stage.NeedsSquad.ShouldBeTrue();
        stage.ComputedAt.ShouldBe(computedAt);
        stage.SlotCount.ShouldBe(5);
        stage.TournamentId.ShouldBe("tournament");
        stage.TournamentStatus.ShouldBe("Completed");
        stage.Entries.ShouldBeEmpty();
    }

    [Fact(DisplayName = "FieldEntryResponseFactory Create should return a Backfill entry by default and a Stage entry when given a source Stage")]
    public void FieldEntryResponseFactory_Create_ShouldReturnBackfillOrStageEntry()
    {
        // Arrange & Act
        var backfill = FieldEntryResponseFactory.Create();
        var staged = FieldEntryResponseFactory.Create(sourceStageId: FieldEntryResponseFactory.ValidSourceStageId);

        // Assert
        backfill.BowlerId.ShouldBe(FieldEntryResponseFactory.ValidBowlerId);
        backfill.BowlerName.ShouldBe(FieldEntryResponseFactory.ValidBowlerName);
        backfill.ViaOverride.ShouldBeFalse();
        backfill.SourceStageId.ShouldBeNull();
        backfill.Place.ShouldBeNull();
        backfill.BackfillPoints.ShouldBe(FieldEntryResponseFactory.ValidBackfillPoints);
        staged.SourceStageId.ShouldBe(FieldEntryResponseFactory.ValidSourceStageId);
        staged.Place.ShouldBe(FieldEntryResponseFactory.ValidPlace);
        staged.BackfillPoints.ShouldBeNull();
    }

    [Fact(DisplayName = "The other advancement response factories Create should return their constants with no arguments")]
    public void OtherResponseFactories_Create_ShouldReturnDefaults()
    {
        // Arrange & Act
        var stageOverride = StageOverrideResponseFactory.Create();
        var outcome = SurvivorOutcomeResponseFactory.Create();
        var position = SeriesPositionResponseFactory.Create();
        var created = CreatedStageOverrideResponseFactory.Create();
        var result = StageResultSummaryResponseFactory.Create();
        var squad = StageSquadSummaryResponseFactory.Create();

        // Assert
        stageOverride.OverrideId.ShouldBe(StageOverrideResponseFactory.ValidOverrideId);
        stageOverride.BowlerId.ShouldBe(StageOverrideResponseFactory.ValidBowlerId);
        stageOverride.BowlerName.ShouldBe(StageOverrideResponseFactory.ValidBowlerName);
        stageOverride.Kind.ShouldBe(StageOverrideResponseFactory.ValidKind);
        stageOverride.Reason.ShouldBe(StageOverrideResponseFactory.ValidReason);
        stageOverride.CreatedBy.ShouldBe(StageOverrideResponseFactory.ValidCreatedBy);
        stageOverride.CreatedAt.ShouldBe(StageOverrideResponseFactory.ValidCreatedAt);
        stageOverride.Applied.ShouldBeTrue();
        outcome.BowlerId.ShouldBe(SurvivorOutcomeResponseFactory.ValidBowlerId);
        outcome.BowlerName.ShouldBe(SurvivorOutcomeResponseFactory.ValidBowlerName);
        outcome.BlockTotal.ShouldBe(SurvivorOutcomeResponseFactory.ValidBlockTotal);
        outcome.Advanced.ShouldBeTrue();
        position.BowlerId.ShouldBe(SeriesPositionResponseFactory.ValidBowlerId);
        position.BowlerName.ShouldBe(SeriesPositionResponseFactory.ValidBowlerName);
        position.Position.ShouldBe(SeriesPositionResponseFactory.ValidPosition);
        position.Bowled.ShouldBeTrue();
        position.IsFinalist.ShouldBeNull();
        created.OverrideId.ShouldBe(CreatedStageOverrideResponseFactory.ValidOverrideId);
        created.Version.ShouldBe(CreatedStageOverrideResponseFactory.ValidVersion);
        result.BowlerId.ShouldBe(StageResultSummaryResponseFactory.ValidBowlerId);
        result.BowlerName.ShouldBe(StageResultSummaryResponseFactory.ValidBowlerName);
        result.Place.ShouldBe(StageResultSummaryResponseFactory.ValidPlace);
        squad.SquadId.ShouldBe(StageSquadSummaryResponseFactory.ValidSquadId);
        squad.Date.ShouldBe(StageSquadSummaryResponseFactory.ValidDate);
        squad.StartTime.ShouldBe(StageSquadSummaryResponseFactory.ValidStartTime);
        squad.HasScores.ShouldBeFalse();
        squad.IsSelected.ShouldBeFalse();
    }

    [Fact(DisplayName = "The other advancement response factories Create should use the given values")]
    public void OtherResponseFactories_Create_ShouldUseGivenValues()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2025, 5, 6, 7, 0, 0, TimeSpan.Zero);

        // Act
        var stageOverride = StageOverrideResponseFactory.Create("o", "b", "Name", "Clear", "why", "me", createdAt, false);
        var outcome = SurvivorOutcomeResponseFactory.Create("b", "Name", null, false);
        var position = SeriesPositionResponseFactory.Create("b", "Name", 4, false, true);
        var created = CreatedStageOverrideResponseFactory.Create("o", 9);
        var result = StageResultSummaryResponseFactory.Create("b", "Name", 3);
        var squad = StageSquadSummaryResponseFactory.Create("s", new DateOnly(2025, 8, 1), new TimeOnly(14, 30), true, true);

        // Assert
        stageOverride.Kind.ShouldBe("Clear");
        stageOverride.CreatedAt.ShouldBe(createdAt);
        stageOverride.Applied.ShouldBeFalse();
        outcome.BlockTotal.ShouldBeNull();
        outcome.Advanced.ShouldBeFalse();
        position.Position.ShouldBe(4);
        position.IsFinalist.ShouldBe(true);
        created.Version.ShouldBe(9);
        result.Place.ShouldBe(3);
        squad.Date.ShouldBe(new DateOnly(2025, 8, 1));
        squad.StartTime.ShouldBe(new TimeOnly(14, 30));
        squad.IsSelected.ShouldBeTrue();
    }

    [Fact(DisplayName = "SeriesAdvancementResponseFactory Bogus should return the requested count, the same data for the same seed and different data for another seed")]
    public void SeriesAdvancementResponseFactory_Bogus_ShouldReturnCount_AndBeDeterministic()
    {
        // Arrange
        const int seed = 411;

        // Act
        var first = SeriesAdvancementResponseFactory.Bogus(2, seed);
        var second = SeriesAdvancementResponseFactory.Bogus(2, seed);
        var other = SeriesAdvancementResponseFactory.Bogus(2, seed + 1);

        // Assert
        first.Count.ShouldBe(2);
        first.ShouldAllBe(response => response.Stages.Count == 3);
        first.Select(response => response.Stages[0].StageId).ShouldBe(second.Select(response => response.Stages[0].StageId));
        first.Select(response => response.Stages[0].StageId).ShouldNotBe(other.Select(response => response.Stages[0].StageId));
        first[0].Stages.Select(stage => stage.Kind).ShouldBe(["Qualifying", "Survivor", "Placement"]);
    }

    [Fact(DisplayName = "The advancement child response factories Bogus should return the requested count, deterministic for a seed")]
    public void ChildResponseFactories_Bogus_ShouldReturnCount_AndBeDeterministic()
    {
        // Arrange
        const int seed = 412;

        // Act
        var entries = FieldEntryResponseFactory.Bogus(3, seed);
        var sameEntries = FieldEntryResponseFactory.Bogus(3, seed);
        var overrides = StageOverrideResponseFactory.Bogus(2, seed);
        var sameOverrides = StageOverrideResponseFactory.Bogus(2, seed);
        var outcomes = SurvivorOutcomeResponseFactory.Bogus(3, seed);
        var sameOutcomes = SurvivorOutcomeResponseFactory.Bogus(3, seed);
        var positions = SeriesPositionResponseFactory.Bogus(3, seed);
        var samePositions = SeriesPositionResponseFactory.Bogus(3, seed);
        var created = CreatedStageOverrideResponseFactory.Bogus(2, seed);
        var sameCreated = CreatedStageOverrideResponseFactory.Bogus(2, seed);
        var results = StageResultSummaryResponseFactory.Bogus(4, seed);
        var sameResults = StageResultSummaryResponseFactory.Bogus(4, seed);
        var squads = StageSquadSummaryResponseFactory.Bogus(3, seed);
        var sameSquads = StageSquadSummaryResponseFactory.Bogus(3, seed);

        // Assert
        entries.Count.ShouldBe(3);
        entries.Select(row => row.BowlerId).ShouldBe(sameEntries.Select(row => row.BowlerId));
        overrides.Count.ShouldBe(2);
        overrides.Select(row => row.OverrideId).ShouldBe(sameOverrides.Select(row => row.OverrideId));
        outcomes.Count.ShouldBe(3);
        outcomes.Select(row => row.BowlerId).ShouldBe(sameOutcomes.Select(row => row.BowlerId));
        positions.Select(row => row.Position).ShouldBe([1, 2, 3]);
        positions.Select(row => row.BowlerId).ShouldBe(samePositions.Select(row => row.BowlerId));
        created.Count.ShouldBe(2);
        created.Select(row => row.OverrideId).ShouldBe(sameCreated.Select(row => row.OverrideId));
        results.Select(row => row.Place).ShouldBe([1, 2, 3, 4]);
        results.Select(row => row.BowlerId).ShouldBe(sameResults.Select(row => row.BowlerId));
        squads.Count.ShouldBe(3);
        squads.Select(row => row.SquadId).ShouldBe(sameSquads.Select(row => row.SquadId));
        squads.Select(row => row.IsSelected).ShouldBe([true, false, false]);
    }
}