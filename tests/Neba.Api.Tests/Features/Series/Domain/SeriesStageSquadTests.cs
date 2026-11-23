using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SeriesStageSquadTests
{
    private static (SeriesStage Stage, SquadId SquadId) ResolvedStage()
    {
        var stage = SeriesStageFactory.Create(StageKind.Survivor, 8);
        var squadId = SquadId.New();
        stage.ResolveSquad(squadId);

        return (stage, squadId);
    }

    private static SeriesStageDefinition Definition(SeriesStage stage, int count, DateOnly date, TimeOnly startTime)
        => SeriesStageDefinitionFactory.Create(
            stageId: stage.Id,
            kind: StageKind.Survivor,
            tournamentId: stage.TournamentId,
            advancingCount: count,
            squadDate: date,
            squadStartTime: startTime);

    private static StageTournamentFacts Facts(SeriesStage stage)
        => StageTournamentFactsFactory.Create(
            tournamentId: stage.TournamentId,
            startDate: stage.SquadDesignation!.Date,
            endDate: stage.SquadDesignation.Date.AddDays(1));

    [Fact(DisplayName = "Update should keep the resolved SquadId when the squad date and start time are unchanged")]
    public void Update_ShouldKeepSquadId_WhenDateAndStartTimeAreUnchanged()
    {
        // Arrange
        var (stage, squadId) = ResolvedStage();
        var designation = stage.SquadDesignation!;
        var definition = Definition(stage, 6, designation.Date, designation.StartTime);

        // Act
        var result = stage.Update(1, definition, Facts(stage));

        // Assert
        result.IsError.ShouldBeFalse();
        stage.SquadDesignation!.SquadId.ShouldBe(squadId);
        stage.AdvancingCount.ShouldBe(6);
    }

    [Fact(DisplayName = "Update should drop the resolved SquadId when the squad date changes")]
    public void Update_ShouldDropSquadId_WhenDateChanges()
    {
        // Arrange
        var (stage, _) = ResolvedStage();
        var designation = stage.SquadDesignation!;
        var newDate = designation.Date.AddDays(1);
        var definition = Definition(stage, 8, newDate, designation.StartTime);

        // Act
        var result = stage.Update(1, definition, Facts(stage));

        // Assert
        result.IsError.ShouldBeFalse();
        stage.SquadDesignation!.SquadId.ShouldBeNull();
        stage.SquadDesignation.Date.ShouldBe(newDate);
    }

    [Fact(DisplayName = "Update should drop the resolved SquadId when the squad start time changes")]
    public void Update_ShouldDropSquadId_WhenStartTimeChanges()
    {
        // Arrange
        var (stage, _) = ResolvedStage();
        var designation = stage.SquadDesignation!;
        var newTime = designation.StartTime.AddHours(1);
        var definition = Definition(stage, 8, designation.Date, newTime);

        // Act
        var result = stage.Update(1, definition, Facts(stage));

        // Assert
        result.IsError.ShouldBeFalse();
        stage.SquadDesignation!.SquadId.ShouldBeNull();
        stage.SquadDesignation.StartTime.ShouldBe(newTime);
    }

    [Fact(DisplayName = "Describe should create an unresolved designation, Resolve should set the squad and Unresolve should clear it")]
    public void SquadDesignation_ShouldResolveAndUnresolve()
    {
        // Arrange
        var designation = SquadDesignationFactory.Create();
        var squadId = SquadId.New();

        // Act
        var resolved = designation.Resolve(squadId);
        var unresolved = resolved.Unresolve();

        // Assert
        designation.IsResolved.ShouldBeFalse();
        resolved.IsResolved.ShouldBeTrue();
        resolved.SquadId.ShouldBe(squadId);
        resolved.Date.ShouldBe(designation.Date);
        resolved.StartTime.ShouldBe(designation.StartTime);
        unresolved.ShouldBe(designation);
    }
}