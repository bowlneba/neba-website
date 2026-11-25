using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.TestFactory.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesAdvancementFactoryTests
{
    [Fact(DisplayName = "StageOverrideFactory Create should return a valid applied Add override with no arguments")]
    public void StageOverrideFactory_Create_ShouldReturnValidOverride()
    {
        // Arrange & Act
        var created = StageOverrideFactory.Create();

        // Assert
        created.Kind.ShouldBe(OverrideKind.Add);
        created.Reason.ShouldBe(StageOverrideFactory.ValidReason);
        created.CreatedBy.ShouldBe(StageOverrideFactory.ValidCreatedBy);
        created.CreatedAt.ShouldBe(StageOverrideFactory.ValidCreatedAt);
        created.CreatedAt.Offset.ShouldBe(TimeSpan.Zero);
        created.Applied.ShouldBeTrue();
    }

    [Fact(DisplayName = "StageOverrideFactory Create should use the arguments it is given")]
    public void StageOverrideFactory_Create_ShouldUseArguments()
    {
        // Arrange
        var bowlerId = BowlerId.New();
        var at = new DateTimeOffset(2025, 4, 1, 8, 0, 0, TimeSpan.Zero);

        // Act
        var created = StageOverrideFactory.Create(OverrideKind.Clear, bowlerId, "Moved away.", "admin-9", at);

        // Assert
        created.Kind.ShouldBe(OverrideKind.Clear);
        created.BowlerId.ShouldBe(bowlerId);
        created.Reason.ShouldBe("Moved away.");
        created.CreatedBy.ShouldBe("admin-9");
        created.CreatedAt.ShouldBe(at);
    }

    [Fact(DisplayName = "StageOverrideFactory Bogus should return the requested count of valid overrides, the same for the same seed")]
    public void StageOverrideFactory_Bogus_ShouldReturnCount_AndBeDeterministic()
    {
        // Arrange
        const int count = 6;
        const int seed = 301;

        // Act
        var first = StageOverrideFactory.Bogus(count, seed);
        var second = StageOverrideFactory.Bogus(count, seed);

        // Assert
        first.Count.ShouldBe(count);
        first.ShouldAllBe(o => o.Applied && o.Reason.Length > 0);
        first.Select(o => o.BowlerId).ShouldBe(second.Select(o => o.BowlerId));
        first.Select(o => o.Kind).ShouldBe(second.Select(o => o.Kind));
    }

    [Fact(DisplayName = "SeriesFieldEntryFactory Create should return a Backfill entry with no arguments")]
    public void SeriesFieldEntryFactory_Create_ShouldReturnBackfillEntry()
    {
        // Arrange & Act
        var created = SeriesFieldEntryFactory.Create();

        // Assert
        created.IsBackfill.ShouldBeTrue();
        created.BackfillPoints.ShouldBe(SeriesFieldEntryFactory.ValidBackfillPoints);
        created.Place.ShouldBeNull();
        created.ViaOverride.ShouldBeFalse();
    }

    [Fact(DisplayName = "SeriesFieldEntryFactory Create should return a Stage entry when a source Stage is given")]
    public void SeriesFieldEntryFactory_Create_ShouldReturnStageEntry_WhenSourceIsGiven()
    {
        // Arrange
        var stageId = SeriesStageId.New();

        // Act
        var created = SeriesFieldEntryFactory.Create(sourceStageId: stageId);

        // Assert
        created.IsBackfill.ShouldBeFalse();
        created.SourceStageId.ShouldBe(stageId);
        created.Place.ShouldBe(SeriesFieldEntryFactory.ValidPlace);
        created.BackfillPoints.ShouldBeNull();
    }

    [Theory(DisplayName = "SeriesFieldEntryFactory Bogus should return the requested count of valid entries for a Backfill or a Stage source")]
    [InlineData(false)]
    [InlineData(true)]
    public void SeriesFieldEntryFactory_Bogus_ShouldReturnCount(bool withStage)
    {
        // Arrange
        const int count = 5;
        SeriesStageId? stageId = withStage ? SeriesStageId.New() : null;

        // Act
        var entries = SeriesFieldEntryFactory.Bogus(count, stageId, seed: 302);

        // Assert
        entries.Count.ShouldBe(count);
        entries.Select(e => e.BowlerId).Distinct().Count().ShouldBe(count);
        entries.ShouldAllBe(e => e.SourceStageId == stageId);
        entries.ShouldAllBe(e => withStage ? e.Place > 0 : e.BackfillPoints > 0);
    }

    [Fact(DisplayName = "SeriesFieldEntryFactory Bogus should return the same bowlers for the same seed")]
    public void SeriesFieldEntryFactory_Bogus_ShouldBeDeterministic()
    {
        // Arrange & Act
        var first = SeriesFieldEntryFactory.Bogus(4, seed: 303);
        var second = SeriesFieldEntryFactory.Bogus(4, seed: 303);

        // Assert
        first.Select(e => e.BowlerId).ShouldBe(second.Select(e => e.BowlerId));
        first.Select(e => e.BackfillPoints).ShouldBe(second.Select(e => e.BackfillPoints));
    }

    [Fact(DisplayName = "SurvivorOutcomeFactory Create should return an advancing outcome with a block total and no arguments")]
    public void SurvivorOutcomeFactory_Create_ShouldReturnValidOutcome()
    {
        // Arrange & Act
        var created = SurvivorOutcomeFactory.Create();

        // Assert
        created.Advanced.ShouldBeTrue();
        created.BlockTotal.ShouldBe(SurvivorOutcomeFactory.ValidBlockTotal);
    }

    [Fact(DisplayName = "SurvivorOutcomeFactory Bogus should return the requested count of valid outcomes, the same for the same seed")]
    public void SurvivorOutcomeFactory_Bogus_ShouldReturnCount_AndBeDeterministic()
    {
        // Arrange
        const int count = 7;

        // Act
        var first = SurvivorOutcomeFactory.Bogus(count, 304);
        var second = SurvivorOutcomeFactory.Bogus(count, 304);

        // Assert
        first.Count.ShouldBe(count);
        first.Select(o => (o.BowlerId, o.BlockTotal, o.Advanced)).ShouldBe(second.Select(o => (o.BowlerId, o.BlockTotal, o.Advanced)));
    }

    [Fact(DisplayName = "SeriesPositionFactory Create should return position 1, bowled, with no finalist flag and no arguments")]
    public void SeriesPositionFactory_Create_ShouldReturnValidPosition()
    {
        // Arrange & Act
        var created = SeriesPositionFactory.Create();

        // Assert
        created.Position.ShouldBe(SeriesPositionFactory.ValidPosition);
        created.Bowled.ShouldBeTrue();
        created.IsFinalist.ShouldBeNull();
    }

    [Fact(DisplayName = "SeriesPositionFactory Bogus should return positions 1 to count, the same for the same seed")]
    public void SeriesPositionFactory_Bogus_ShouldReturnCount_AndBeDeterministic()
    {
        // Arrange
        const int count = 6;

        // Act
        var first = SeriesPositionFactory.Bogus(count, 305);
        var second = SeriesPositionFactory.Bogus(count, 305);

        // Assert
        first.Select(p => p.Position).ShouldBe([1, 2, 3, 4, 5, 6]);
        first.Select(p => p.BowlerId).ShouldBe(second.Select(p => p.BowlerId));
    }

    [Theory(DisplayName = "StageTournamentFactsFactory Create should take the tournament status, Scheduled by default")]
    [InlineData(null, "Scheduled")]
    [InlineData("Cancelled", "Cancelled")]
    [InlineData("Completed", "Completed")]
    public void StageTournamentFactsFactory_Create_ShouldTakeStatus(string? status, string expected)
    {
        // Arrange
        TournamentStatus? requested = status is null ? null : TournamentStatus.FromName(status);

        // Act
        var facts = StageTournamentFactsFactory.Create(status: requested);

        // Assert
        facts.Status.Name.ShouldBe(expected);
    }

    [Fact(DisplayName = "SeriesFactory CreateWithAdvancement should return a Series with one Computed Qualifying Stage and its facts by default")]
    public void SeriesFactory_CreateWithAdvancement_ShouldReturnComputedQualifyingStage_ByDefault()
    {
        // Arrange & Act
        var (series, facts) = SeriesFactory.CreateWithAdvancement();

        // Assert
        var stage = series.Stages.Single();
        stage.Kind.ShouldBe(StageKind.Qualifying);
        stage.Status.ShouldBe(StageStatus.Computed);
        stage.SlotCount.ShouldBe(SeriesStageFactory.ValidAdvancingCount);
        series.FieldEntries.Count.ShouldBe(SeriesStageFactory.ValidAdvancingCount);
        series.BackfillAsOf.ShouldNotBeNull();
        series.Version.ShouldBe(1);
        facts.Tournaments.ContainsKey(stage.TournamentId).ShouldBeTrue();
    }

    [Fact(DisplayName = "SeriesFactory CreateWithAdvancement should compute every Stage kind in a full chain")]
    public void SeriesFactory_CreateWithAdvancement_ShouldComputeEveryStageKind()
    {
        // Arrange & Act
        var (series, _) = SeriesFactory.CreateWithAdvancement(
            [StageKind.Qualifying, StageKind.Qualifying, StageKind.Survivor, StageKind.Survivor, StageKind.Placement]);

        // Assert
        series.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed);
        series.FieldEntries.Count.ShouldBe(2 * SeriesStageFactory.ValidAdvancingCount);
        series.Stages.Where(stage => stage.Kind == StageKind.Survivor).ShouldAllBe(stage => stage.SurvivorOutcomes.Count > 0);
        series.Positions.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "SeriesFactory CreateWithAdvancement should make every Stage Official, Placement included")]
    public void SeriesFactory_CreateWithAdvancement_ShouldMakeStagesOfficial_WhenAsked()
    {
        // Arrange

        // Act
        var (series, _) = SeriesFactory.CreateWithAdvancement(
            [StageKind.Qualifying, StageKind.Survivor, StageKind.Placement], official: true);

        // Assert
        series.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Official);
    }

    [Fact(DisplayName = "SeriesFactory CreateWithAdvancement should stamp the Stages with the time it is given")]
    public void SeriesFactory_CreateWithAdvancement_ShouldUseTheGivenTime()
    {
        // Arrange
        var at = new DateTimeOffset(2025, 9, 1, 6, 30, 0, TimeSpan.Zero);

        // Act
        var (series, _) = SeriesFactory.CreateWithAdvancement(now: at);

        // Assert
        series.Stages.Single().ComputedAt.ShouldBe(at);
    }

    [Fact(DisplayName = "SeriesFactory CreateWithAdvancement should return facts that advance the Series again without a change")]
    public void SeriesFactory_CreateWithAdvancement_ShouldReturnFactsThatReproduceTheOutput()
    {
        // Arrange
        var (series, facts) = SeriesFactory.CreateWithAdvancement([StageKind.Qualifying, StageKind.Survivor]);
        var first = series.Stages.OrderBy(stage => stage.Order).First();

        // Act
        var result = series.Advance(first.TournamentId, facts, DateTimeOffset.UtcNow);

        // Assert
        result.Changed.ShouldBeFalse();
    }
}