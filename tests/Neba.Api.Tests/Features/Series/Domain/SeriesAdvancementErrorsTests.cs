using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SeriesAdvancementErrorsTests
{
    [Fact(DisplayName = "ConfirmationRequired should say a single Official Stage is official and carry its order")]
    public void ConfirmationRequired_ShouldUseSingularWording_WhenOneStageIsOfficial()
    {
        // Arrange & Act
        var error = SeriesAdvancementErrors.ConfirmationRequired([3]);

        // Assert
        error.Description.ShouldStartWith("Tournament 3 is official. Recomputing resets it to Computed.");
        ((int[])error.Metadata![SeriesAdvancementErrors.OfficialStageOrdersKey]).ShouldBe([3]);
    }

    [Fact(DisplayName = "ConfirmationRequired should list several Official Stages and say they are official")]
    public void ConfirmationRequired_ShouldUsePluralWording_WhenSeveralStagesAreOfficial()
    {
        // Arrange & Act
        var error = SeriesAdvancementErrors.ConfirmationRequired([2, 3, 5]);

        // Assert
        error.Description.ShouldStartWith("Tournaments 2, 3, 5 are official. Recomputing resets them to Computed.");
        ((int[])error.Metadata![SeriesAdvancementErrors.OfficialStageOrdersKey]).ShouldBe([2, 3, 5]);
    }

    [Fact(DisplayName = "StageNotComputed should tell the admin to recompute for an Official Stage and to wait for a Pending one")]
    public void StageNotComputed_ShouldExplainTheStatus()
    {
        // Arrange & Act
        var official = SeriesAdvancementErrors.StageNotComputed(StageStatus.Official);
        var pending = SeriesAdvancementErrors.StageNotComputed(StageStatus.Pending);

        // Assert
        official.Description.ShouldBe("This tournament is official. Recompute it from the Series page before changing it.");
        pending.Description.ShouldBe("This tournament has not been computed yet.");
        official.Code.ShouldBe(pending.Code);
    }

    [Fact(DisplayName = "StageNotFound should be NotFound and carry the Stage ID")]
    public void StageNotFound_ShouldCarryTheStageId()
    {
        // Arrange
        var id = SeriesStageId.New();

        // Act
        var error = SeriesAdvancementErrors.StageNotFound(id);

        // Assert
        error.Type.ShouldBe(ErrorType.NotFound);
        error.Metadata!["StageId"].ShouldBe(id.ToString());
    }

    [Fact(DisplayName = "OverrideReasonTooLong should state the limit")]
    public void OverrideReasonTooLong_ShouldStateTheLimit()
    {
        // Arrange & Act
        var error = SeriesAdvancementErrors.OverrideReasonTooLong(500);

        // Assert
        error.Description.ShouldBe("The reason must be 500 characters or fewer.");
    }

    [Fact(DisplayName = "Advancement errors should map to the status class the plan gives them")]
    public void AdvancementErrors_ShouldHaveTheExpectedTypes()
    {
        // Arrange & Act & Assert
        SeriesAdvancementErrors.VersionConflict.Type.ShouldBe(ErrorType.Conflict);
        SeriesAdvancementErrors.ConcurrentChange.Type.ShouldBe(ErrorType.Conflict);
        SeriesAdvancementErrors.Busy.Type.ShouldBe(ErrorType.Conflict);
        SeriesAdvancementErrors.BowlerAlreadyInField.Type.ShouldBe(ErrorType.Conflict);
        SeriesAdvancementErrors.OverrideExists.Type.ShouldBe(ErrorType.Conflict);
        SeriesAdvancementErrors.NotQualifyingStage.Type.ShouldBe(ErrorType.Validation);
        SeriesAdvancementErrors.NoResultRow.Type.ShouldBe(ErrorType.Validation);
        SeriesAdvancementErrors.BowlerNotInSlot.Type.ShouldBe(ErrorType.Validation);
        SeriesAdvancementErrors.OverrideReasonRequired.Type.ShouldBe(ErrorType.Validation);
        SeriesAdvancementErrors.StageNotSurvivor.Type.ShouldBe(ErrorType.Validation);
        SeriesAdvancementErrors.SquadNotInTournament.Type.ShouldBe(ErrorType.Validation);
    }
}