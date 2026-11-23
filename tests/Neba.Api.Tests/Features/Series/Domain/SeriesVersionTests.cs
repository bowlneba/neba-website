using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.TestFactory.Storage;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SeriesVersionTests
{
    private static readonly SeriesFinaleDefinition Finale = SeriesFinaleDefinitionFactory.Create();

    private static List<SeriesStageDefinition> Submitted(SeriesAggregate series, SeriesTestScenario scenario)
        => [.. series.Stages.OrderBy(stage => stage.Order).Select((stage, index) => scenario.Stages[index] with { StageId = stage.Id })];

    private static (AdvancementHarness Harness, BowlerId Extra) ComputedWithExtraBowler()
    {
        var harness = AdvancementHarness.Create(StageKind.Qualifying);
        var extra = BowlerId.New();
        harness.SetResults(1, [.. harness.Tournament(1).Results, StageResultRowFactory.Create(extra, AdvancementHarness.SlotCount + 1)]);
        _ = harness.Advance();

        return (harness, extra);
    }

    [Fact(DisplayName = "Create should start the Series at Version 0 even though it applies a Stage list")]
    public void Create_ShouldStartAtVersionZero()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Qualifying(1, 24).Placement(9, 13);

        // Act
        var result = scenario.Create();

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Version.ShouldBe(0);
    }

    [Fact(DisplayName = "Advance should bump Version when it computes a Pending Stage")]
    public void Advance_ShouldBumpVersion_WhenItComputesAPendingStage()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying);

        // Act
        var result = harness.Advance();

        // Assert
        result.Changed.ShouldBeTrue();
        result.Version.ShouldBe(1);
        harness.Series.Version.ShouldBe(1);
    }

    [Fact(DisplayName = "Advance should bump Version when a Computed Stage's output changes")]
    public void Advance_ShouldBumpVersion_WhenComputedOutputChanges()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        harness.SwapFirstTwoPlaces(1);

        // Act
        var result = harness.Advance();

        // Assert
        result.Changed.ShouldBeTrue();
        harness.Series.Version.ShouldBe(2);
    }

    [Fact(DisplayName = "Advance should leave Version alone when nothing changed")]
    public void Advance_ShouldLeaveVersionAlone_WhenNothingChanged()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();

        // Act
        var result = harness.Advance();

        // Assert
        result.Changed.ShouldBeFalse();
        result.StatusChanged.ShouldBeFalse();
        harness.Series.Version.ShouldBe(1);
    }

    [Fact(DisplayName = "Advance should return None and leave Version alone when no Stage uses the tournament")]
    public void Advance_ShouldReturnNone_WhenNoStageUsesTheTournament()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();

        // Act
        var result = harness.Series.Advance(TournamentId.New(), harness.Facts, AdvancementHarness.Now);

        // Assert
        result.ShouldBe(AdvanceResult.None);
        harness.Series.Version.ShouldBe(1);
    }

    [Fact(DisplayName = "Recompute should bump Version when the output changed")]
    public void Recompute_ShouldBumpVersion_WhenOutputChanged()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        harness.SwapFirstTwoPlaces(1);

        // Act
        var result = harness.Recompute(1);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeTrue();
        harness.Series.Version.ShouldBe(2);
    }

    [Fact(DisplayName = "Recompute should leave Version alone when nothing changed")]
    public void Recompute_ShouldLeaveVersionAlone_WhenNothingChanged()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();

        // Act
        var result = harness.Recompute(1);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeFalse();
        harness.Series.Version.ShouldBe(1);
    }

    [Fact(DisplayName = "PickSquad should bump Version when the squad changes")]
    public void PickSquad_ShouldBumpVersion_WhenSquadChanges()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Survivor);
        var designated = harness.Tournament(2).Squads.Single();
        var other = StageSquadFactFactory.Create(
            localDate: designated.LocalDate, localStartTime: new TimeOnly(18, 0), totals: designated.Totals);
        harness.SetSquads(2, [designated, other]).Advanced();

        // Act
        var result = harness.PickSquad(2, other.SquadId);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeTrue();
        harness.Series.Version.ShouldBe(2);
    }

    [Fact(DisplayName = "PickSquad should leave Version alone when the Stage already uses the squad")]
    public void PickSquad_ShouldLeaveVersionAlone_WhenSquadIsUnchanged()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Survivor).Advanced();
        var current = harness.Stage(2).SquadDesignation!.SquadId!.Value;

        // Act
        var result = harness.PickSquad(2, current);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeFalse();
        harness.Series.Version.ShouldBe(1);
    }

    [Fact(DisplayName = "AddOverride should bump Version when it saves an override")]
    public void AddOverride_ShouldBumpVersion_WhenItSavesAnOverride()
    {
        // Arrange
        var (harness, extra) = ComputedWithExtraBowler();

        // Act
        var result = harness.AddOverride(1, extra);

        // Assert
        result.IsError.ShouldBeFalse();
        harness.Series.Version.ShouldBe(2);
    }

    [Fact(DisplayName = "AddOverride should leave Version alone when it returns an error")]
    public void AddOverride_ShouldLeaveVersionAlone_WhenItReturnsAnError()
    {
        // Arrange
        var (harness, _) = ComputedWithExtraBowler();

        // Act
        var result = harness.AddOverride(1, BowlerId.New());

        // Assert
        result.IsError.ShouldBeTrue();
        harness.Series.Version.ShouldBe(1);
    }

    [Fact(DisplayName = "ClearSlot should bump Version when it saves an override")]
    public void ClearSlot_ShouldBumpVersion_WhenItSavesAnOverride()
    {
        // Arrange
        var (harness, _) = ComputedWithExtraBowler();

        // Act
        var result = harness.ClearSlot(1, harness.Qualifiers(1)[0]);

        // Assert
        result.IsError.ShouldBeFalse();
        harness.Series.Version.ShouldBe(2);
    }

    [Fact(DisplayName = "ClearSlot should leave Version alone when it returns an error")]
    public void ClearSlot_ShouldLeaveVersionAlone_WhenItReturnsAnError()
    {
        // Arrange
        var (harness, _) = ComputedWithExtraBowler();

        // Act
        var result = harness.ClearSlot(1, BowlerId.New());

        // Assert
        result.IsError.ShouldBeTrue();
        harness.Series.Version.ShouldBe(1);
    }

    [Fact(DisplayName = "RemoveOverride should bump Version when it removes an override")]
    public void RemoveOverride_ShouldBumpVersion_WhenItRemovesAnOverride()
    {
        // Arrange
        var (harness, extra) = ComputedWithExtraBowler();
        var overrideId = harness.AddOverride(1, extra).Value.OverrideId!.Value;

        // Act
        var result = harness.RemoveOverride(1, overrideId);

        // Assert
        result.IsError.ShouldBeFalse();
        harness.Series.Version.ShouldBe(3);
    }

    [Fact(DisplayName = "RemoveOverride should leave Version alone when the override is already gone")]
    public void RemoveOverride_ShouldLeaveVersionAlone_WhenOverrideIsMissing()
    {
        // Arrange
        var (harness, _) = ComputedWithExtraBowler();

        // Act
        var result = harness.RemoveOverride(1, StageOverrideId.New());

        // Assert
        result.IsError.ShouldBeFalse();
        harness.Series.Version.ShouldBe(1);
    }

    [Fact(DisplayName = "MakeReadyOfficial should bump Version when a Stage becomes Official")]
    public void MakeReadyOfficial_ShouldBumpVersion_WhenAStageBecomesOfficial()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();

        // Act
        var result = harness.MakeReadyOfficial();

        // Assert
        result.Changed.ShouldBeTrue();
        harness.Series.Version.ShouldBe(2);
    }

    [Fact(DisplayName = "MakeOfficial should leave Version alone when the Stage is already Official")]
    public void MakeOfficial_ShouldLeaveVersionAlone_WhenStageIsAlreadyOfficial()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        _ = harness.MakeReadyOfficial();
        var version = harness.Series.Version;

        // Act
        var result = harness.Series.MakeOfficial(harness.Stage(1).Id, version, AdvancementHarness.Now);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeFalse();
        harness.Series.Version.ShouldBe(version);
    }

    [Fact(DisplayName = "ReplaceConfiguration should bump Version when the Stages change")]
    public void ReplaceConfiguration_ShouldBumpVersion_WhenStagesChange()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Qualifying(1, 24).Placement(9, 13);
        var series = scenario.Create().Value;
        var changed = Submitted(series, scenario);
        changed[0] = changed[0] with { AdvancingCount = 7 };

        // Act
        var result = series.ReplaceConfiguration(changed, null, scenario.Tournaments);

        // Assert
        result.IsError.ShouldBeFalse();
        series.Version.ShouldBe(1);
    }

    [Fact(DisplayName = "ReplaceConfiguration should leave Version alone when the Stages are unchanged")]
    public void ReplaceConfiguration_ShouldLeaveVersionAlone_WhenStagesAreUnchanged()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Qualifying(1, 24).Placement(9, 13);
        var series = scenario.Create().Value;

        // Act
        var result = series.ReplaceConfiguration(Submitted(series, scenario), null, scenario.Tournaments);

        // Assert
        result.IsError.ShouldBeFalse();
        series.Version.ShouldBe(0);
    }

    [Fact(DisplayName = "ReplaceConfiguration should leave Version alone when only the Finale is rescheduled")]
    public void ReplaceConfiguration_ShouldLeaveVersionAlone_WhenOnlyTheFinaleIsRescheduled()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Qualifying(1, 24).Placement(9, 13);
        var series = scenario.Create(Finale).Value;
        var stages = Submitted(series, scenario);
        stages[0] = stages[0] with { AdvancingCount = 7 };
        _ = series.ReplaceConfiguration(stages, Finale, scenario.Tournaments);
        var rescheduled = SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 10, 4), startTime: new TimeOnly(18, 30));

        // Act
        var result = series.ReplaceConfiguration(stages, rescheduled, scenario.Tournaments);

        // Assert
        result.IsError.ShouldBeFalse();
        series.Finale!.Date.ShouldBe(new DateOnly(2025, 10, 4));
        series.Version.ShouldBe(1);
    }

    [Fact(DisplayName = "Rename and ChangeLogo should leave Version alone")]
    public void RenameAndChangeLogo_ShouldLeaveVersionAlone()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();

        // Act
        var renamed = harness.Series.Rename("New name");
        harness.Series.ChangeLogo(StoredFileFactory.Create());

        // Assert
        renamed.IsError.ShouldBeFalse();
        harness.Series.Version.ShouldBe(1);
    }
}