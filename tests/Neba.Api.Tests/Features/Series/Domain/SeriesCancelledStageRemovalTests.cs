using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SeriesCancelledStageRemovalTests
{
    // Stage 1 Qualifying, Stage 2 Qualifying, Stage 3 Qualifying, Stage 4 Placement. Stage 1 is Computed, which locks the Series.
    private static SeriesTestScenario Scenario()
        => new SeriesTestScenario().Qualifying(1, 24).Qualifying(3, 22).Qualifying(5, 17).Placement(9, 13);

    private static SeriesAggregate Locked(SeriesTestScenario scenario)
    {
        var series = scenario.Create().Value;
        series.Stages.First(stage => stage.Order == 1).Status = StageStatus.Computed;

        return series;
    }

    private static void Cancel(SeriesTestScenario scenario, int stageNumber)
    {
        var id = scenario.Stages[stageNumber - 1].TournamentId;
        scenario.Tournaments[id] = scenario.Tournaments[id] with { Status = TournamentStatus.Cancelled };
    }

    private static List<SeriesStageDefinition> Submitted(SeriesAggregate series, SeriesTestScenario scenario, params int[] removedStageNumbers)
        => [.. series.Stages
            .OrderBy(stage => stage.Order)
            .Select((stage, index) => (Number: index + 1, Definition: scenario.Stages[index] with { StageId = stage.Id }))
            .Where(item => !removedStageNumbers.Contains(item.Number))
            .Select(item => item.Definition)];

    [Fact(DisplayName = "ReplaceConfiguration should remove a Pending Stage whose tournament is Cancelled from a locked Series")]
    public void ReplaceConfiguration_ShouldRemoveCancelledPendingStage_WhenLocked()
    {
        // Arrange
        var scenario = Scenario();
        var series = Locked(scenario);
        Cancel(scenario, 2);
        var keptIds = series.Stages.Where(stage => stage.Order != 2).Select(stage => stage.Id).ToList();

        // Act
        var result = series.ReplaceConfiguration(Submitted(series, scenario, 2), null, scenario.Tournaments);

        // Assert
        result.IsError.ShouldBeFalse();
        series.Stages.OrderBy(stage => stage.Order).Select(stage => stage.Id).ShouldBe(keptIds);
        series.Stages.OrderBy(stage => stage.Order).Select(stage => stage.Order).ShouldBe([1, 2, 3]);
        series.Stages.First(stage => stage.Order == 1).Status.ShouldBe(StageStatus.Computed);
        series.Version.ShouldBe(1);
    }

    [Fact(DisplayName = "ReplaceConfiguration should remove two Cancelled Pending Stages in one change")]
    public void ReplaceConfiguration_ShouldRemoveTwoCancelledPendingStages_WhenLocked()
    {
        // Arrange
        var scenario = Scenario();
        var series = Locked(scenario);
        Cancel(scenario, 2);
        Cancel(scenario, 3);

        // Act
        var result = series.ReplaceConfiguration(Submitted(series, scenario, 2, 3), null, scenario.Tournaments);

        // Assert
        result.IsError.ShouldBeFalse();
        series.Stages.Count.ShouldBe(2);
        series.Stages.Select(stage => stage.Kind).ShouldBe([StageKind.Qualifying, StageKind.Placement], ignoreOrder: true);
    }

    [Theory(DisplayName = "ReplaceConfiguration should return Locked when the removed Stage's tournament is not Cancelled")]
    [InlineData("Scheduled")]
    [InlineData("Completed")]
    [InlineData("Truncated")]
    public void ReplaceConfiguration_ShouldReturnLocked_WhenRemovedStageTournamentIsNotCancelled(string status)
    {
        // Arrange
        var scenario = Scenario();
        var series = Locked(scenario);
        var id = scenario.Stages[1].TournamentId;
        scenario.Tournaments[id] = scenario.Tournaments[id] with { Status = TournamentStatus.FromName(status) };

        // Act
        var result = series.ReplaceConfiguration(Submitted(series, scenario, 2), null, scenario.Tournaments);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Locked");
        series.Stages.Count.ShouldBe(4);
        series.Version.ShouldBe(0);
    }

    [Fact(DisplayName = "ReplaceConfiguration should return Locked when the facts do not hold the removed Stage's tournament")]
    public void ReplaceConfiguration_ShouldReturnLocked_WhenRemovedStageFactsAreMissing()
    {
        // Arrange
        var scenario = Scenario();
        var series = Locked(scenario);
        var submitted = Submitted(series, scenario, 2);
        _ = scenario.Tournaments.Remove(scenario.Stages[1].TournamentId);

        // Act
        var result = series.ReplaceConfiguration(submitted, null, scenario.Tournaments);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Locked");
        series.Stages.Count.ShouldBe(4);
    }

    [Fact(DisplayName = "ReplaceConfiguration should return Locked when the Cancelled Stage is not Pending")]
    public void ReplaceConfiguration_ShouldReturnLocked_WhenCancelledStageIsNotPending()
    {
        // Arrange
        var scenario = Scenario();
        var series = Locked(scenario);
        Cancel(scenario, 1);

        // Act
        var result = series.ReplaceConfiguration(Submitted(series, scenario, 1), null, scenario.Tournaments);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Locked");
        series.Stages.Count.ShouldBe(4);
    }

    [Fact(DisplayName = "ReplaceConfiguration should return Locked when one removed Stage is Cancelled and another is not")]
    public void ReplaceConfiguration_ShouldReturnLocked_WhenOnlySomeRemovedStagesAreCancelled()
    {
        // Arrange
        var scenario = Scenario();
        var series = Locked(scenario);
        Cancel(scenario, 2);

        // Act
        var result = series.ReplaceConfiguration(Submitted(series, scenario, 2, 3), null, scenario.Tournaments);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Locked");
        series.Stages.Count.ShouldBe(4);
    }

    [Fact(DisplayName = "ReplaceConfiguration should return Locked when a kept Stage changes alongside the removal")]
    public void ReplaceConfiguration_ShouldReturnLocked_WhenAKeptStageChanges()
    {
        // Arrange
        var scenario = Scenario();
        var series = Locked(scenario);
        Cancel(scenario, 2);
        var submitted = Submitted(series, scenario, 2);
        submitted[0] = submitted[0] with { AdvancingCount = 7 };

        // Act
        var result = series.ReplaceConfiguration(submitted, null, scenario.Tournaments);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Locked");
        series.Stages.First(stage => stage.Order == 1).AdvancingCount.ShouldBe(8);
    }

    [Fact(DisplayName = "ReplaceConfiguration should return Locked when a new Stage is added alongside the removal")]
    public void ReplaceConfiguration_ShouldReturnLocked_WhenAStageIsAddedAlongsideTheRemoval()
    {
        // Arrange
        var scenario = Scenario();
        var series = Locked(scenario);
        Cancel(scenario, 2);
        var submitted = Submitted(series, scenario, 2);
        var added = new SeriesTestScenario(scenario.SeasonId).Qualifying(6, 14);
        scenario.Tournaments[added.Stages[0].TournamentId] = added.Tournaments[added.Stages[0].TournamentId];
        submitted.Insert(2, added.Stages[0]);

        // Act
        var result = series.ReplaceConfiguration(submitted, null, scenario.Tournaments);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Locked");
        series.Stages.Count.ShouldBe(4);
    }

    [Fact(DisplayName = "ReplaceConfiguration should return Locked when the kept Stages are reordered")]
    public void ReplaceConfiguration_ShouldReturnLocked_WhenKeptStagesAreReordered()
    {
        // Arrange
        var scenario = Scenario();
        var series = Locked(scenario);
        Cancel(scenario, 2);
        var submitted = Submitted(series, scenario, 2);
        submitted.Reverse();

        // Act
        var result = series.ReplaceConfiguration(submitted, null, scenario.Tournaments);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Locked");
        series.Stages.Count.ShouldBe(4);
    }

    [Fact(DisplayName = "ReplaceConfiguration should return Locked when a Finale is added alongside the removal")]
    public void ReplaceConfiguration_ShouldReturnLocked_WhenAFinaleIsAddedAlongsideTheRemoval()
    {
        // Arrange
        var scenario = Scenario();
        var series = Locked(scenario);
        Cancel(scenario, 2);

        // Act
        var result = series.ReplaceConfiguration(
            Submitted(series, scenario, 2), SeriesFinaleDefinitionFactory.Create(), scenario.Tournaments);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Locked");
        series.Finale.ShouldBeNull();
        series.Stages.Count.ShouldBe(4);
    }

    [Fact(DisplayName = "ReplaceConfiguration should return Locked when a Stage is removed from a locked Series and nothing is Cancelled")]
    public void ReplaceConfiguration_ShouldReturnLocked_WhenNothingIsCancelled()
    {
        // Arrange
        var scenario = Scenario();
        var series = Locked(scenario);

        // Act
        var result = series.ReplaceConfiguration(Submitted(series, scenario, 4), null, scenario.Tournaments);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Locked");
        series.Stages.Count.ShouldBe(4);
    }
}