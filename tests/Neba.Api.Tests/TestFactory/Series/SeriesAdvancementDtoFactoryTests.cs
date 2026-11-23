using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.TestFactory.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesAdvancementDtoFactoryTests
{
    [Fact(DisplayName = "SeriesAdvancementDtoFactory Create should return the constants, no Backfill date, and no Stages or positions with no arguments")]
    public void SeriesAdvancementDtoFactory_Create_ShouldReturnDefaults()
    {
        // Arrange & Act
        var dto = SeriesAdvancementDtoFactory.Create();

        // Assert
        dto.Version.ShouldBe(SeriesAdvancementDtoFactory.ValidVersion);
        dto.TargetField.ShouldBe(SeriesAdvancementDtoFactory.ValidTargetField);
        dto.BackfillAsOf.ShouldBeNull();
        dto.Stages.ShouldBeEmpty();
        dto.Positions.ShouldBeEmpty();
    }

    [Fact(DisplayName = "StageAdvancementDtoFactory Create should return a Pending Qualifying Stage with no output with no arguments")]
    public void StageAdvancementDtoFactory_Create_ShouldReturnDefaults()
    {
        // Arrange & Act
        var dto = StageAdvancementDtoFactory.Create();

        // Assert
        dto.Order.ShouldBe(StageAdvancementDtoFactory.ValidOrder);
        dto.Kind.ShouldBe(StageAdvancementDtoFactory.ValidKind);
        dto.Status.ShouldBe(StageAdvancementDtoFactory.ValidStatus);
        dto.TournamentStatus.ShouldBe(StageAdvancementDtoFactory.ValidTournamentStatus);
        dto.IsStale.ShouldBeFalse();
        dto.NeedsSquad.ShouldBeFalse();
        dto.ComputedAt.ShouldBeNull();
        dto.CanMakeOfficial.ShouldBeFalse();
        dto.OfficialAt.ShouldBeNull();
        dto.SlotCount.ShouldBeNull();
        dto.VacantSlotCount.ShouldBeNull();
        dto.ResolvedSquadId.ShouldBeNull();
        dto.Entries.ShouldBeEmpty();
        dto.Overrides.ShouldBeEmpty();
        dto.SurvivorOutcomes.ShouldBeEmpty();
    }

    [Fact(DisplayName = "FieldEntryDtoFactory Create should return a Backfill entry by default and a Stage entry when given a source Stage")]
    public void FieldEntryDtoFactory_Create_ShouldReturnBackfillOrStageEntry()
    {
        // Arrange
        var stageId = SeriesStageId.New();

        // Act
        var backfill = FieldEntryDtoFactory.Create();
        var staged = FieldEntryDtoFactory.Create(sourceStageId: stageId);

        // Assert
        backfill.SourceStageId.ShouldBeNull();
        backfill.Place.ShouldBeNull();
        backfill.BackfillPoints.ShouldBe(FieldEntryDtoFactory.ValidBackfillPoints);
        staged.SourceStageId.ShouldBe(stageId);
        staged.Place.ShouldBe(FieldEntryDtoFactory.ValidPlace);
        staged.BackfillPoints.ShouldBeNull();
    }

    [Fact(DisplayName = "The other advancement DTO factories Create should return their constants with no arguments")]
    public void OtherDtoFactories_Create_ShouldReturnDefaults()
    {
        // Arrange & Act
        var stageOverride = StageOverrideDtoFactory.Create();
        var outcome = SurvivorOutcomeDtoFactory.Create();
        var position = SeriesPositionDtoFactory.Create();
        var result = StageResultSummaryDtoFactory.Create();
        var squad = StageSquadSummaryDtoFactory.Create();

        // Assert
        stageOverride.Kind.ShouldBe(StageOverrideDtoFactory.ValidKind);
        stageOverride.Reason.ShouldBe(StageOverrideDtoFactory.ValidReason);
        stageOverride.CreatedAt.ShouldBe(StageOverrideDtoFactory.ValidCreatedAt);
        stageOverride.Applied.ShouldBeTrue();
        outcome.BlockTotal.ShouldBe(SurvivorOutcomeDtoFactory.ValidBlockTotal);
        outcome.Advanced.ShouldBeTrue();
        SurvivorOutcomeDtoFactory.Create(blockTotal: null).BlockTotal.ShouldBeNull();
        position.Position.ShouldBe(SeriesPositionDtoFactory.ValidPosition);
        position.Bowled.ShouldBeTrue();
        position.IsFinalist.ShouldBeNull();
        result.Place.ShouldBe(StageResultSummaryDtoFactory.ValidPlace);
        squad.LocalDate.ShouldBe(StageSquadSummaryDtoFactory.ValidLocalDate);
        squad.LocalStartTime.ShouldBe(StageSquadSummaryDtoFactory.ValidLocalStartTime);
        squad.HasScores.ShouldBeFalse();
        squad.IsSelected.ShouldBeFalse();
    }

    [Fact(DisplayName = "Bogus should return the requested count, the same data for the same seed and different data for another seed")]
    public void Bogus_ShouldReturnCount_AndBeDeterministic()
    {
        // Arrange
        const int seed = 401;

        // Act
        var first = SeriesAdvancementDtoFactory.Bogus(2, seed);
        var second = SeriesAdvancementDtoFactory.Bogus(2, seed);
        var other = SeriesAdvancementDtoFactory.Bogus(2, seed + 1);

        // Assert
        first.Count.ShouldBe(2);
        first.ShouldAllBe(dto => dto.Stages.Count == 3);
        first.Select(dto => dto.Stages[0].StageId).ShouldBe(second.Select(dto => dto.Stages[0].StageId));
        first.Select(dto => dto.Stages[0].StageId).ShouldNotBe(other.Select(dto => dto.Stages[0].StageId));
        first[0].Stages.Select(stage => stage.Kind).ShouldBe(["Qualifying", "Survivor", "Placement"]);
    }

    [Fact(DisplayName = "The list DTO factories Bogus should return the requested count, deterministic for a seed")]
    public void ListDtoFactories_Bogus_ShouldReturnCount_AndBeDeterministic()
    {
        // Arrange
        const int seed = 402;

        // Act
        var results = StageResultSummaryDtoFactory.Bogus(4, seed);
        var sameResults = StageResultSummaryDtoFactory.Bogus(4, seed);
        var squads = StageSquadSummaryDtoFactory.Bogus(3, seed);
        var sameSquads = StageSquadSummaryDtoFactory.Bogus(3, seed);

        // Assert
        results.Select(row => row.Place).ShouldBe([1, 2, 3, 4]);
        results.Select(row => row.BowlerId).ShouldBe(sameResults.Select(row => row.BowlerId));
        squads.Count.ShouldBe(3);
        squads.Select(row => row.SquadId).ShouldBe(sameSquads.Select(row => row.SquadId));
        squads.Select(row => row.LocalDate).ShouldBe(sameSquads.Select(row => row.LocalDate));
        squads.Select(row => row.IsSelected).ShouldBe([true, false, false]);
    }
}