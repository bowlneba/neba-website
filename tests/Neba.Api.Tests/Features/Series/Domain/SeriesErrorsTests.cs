using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SeriesErrorsTests
{
    [Fact(DisplayName = "Stage errors should name the Stage and tournament and carry StageOrder metadata")]
    public void StageErrors_ShouldNameStageAndCarryOrder()
    {
        // Arrange & Act
        var error = SeriesErrors.TournamentNotInSeason(9, "Callahan's");

        // Assert
        error.Description.ShouldStartWith("Tournament 9 (Callahan's): ");
        error.Metadata![SeriesErrors.StageOrderKey].ShouldBe(9);
        error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact(DisplayName = "A Stage error without a tournament name should name only the Stage")]
    public void StageErrors_ShouldOmitTournament_WhenNameIsUnknown()
    {
        // Arrange & Act
        var error = SeriesErrors.StageNotFound(2);

        // Assert
        error.Description.ShouldStartWith("Tournament 2: ");
    }

    [Fact(DisplayName = "TournamentInAnotherSeries should be a Conflict")]
    public void TournamentInAnotherSeries_ShouldBeConflict()
    {
        // Arrange & Act
        var error = SeriesErrors.TournamentInAnotherSeries(1, "T", "Other");

        // Assert
        error.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact(DisplayName = "Finale errors should carry the PropertyKey metadata and no StageOrder")]
    public void FinaleErrors_ShouldCarryPropertyKeyOnly()
    {
        // Arrange & Act
        var count = SeriesErrors.FinaleCountNotPositive;
        var exceeds = SeriesErrors.FinaleCountExceedsSurvivor(30, 20);
        var center = SeriesErrors.FinaleBowlingCenterNotAvailable("01948");

        // Assert
        count.Metadata![SeriesErrors.PropertyKey].ShouldBe("Series.Finale.FinalistCount");
        exceeds.Metadata![SeriesErrors.PropertyKey].ShouldBe("Series.Finale.FinalistCount");
        center.Metadata![SeriesErrors.PropertyKey].ShouldBe("Series.Finale.BowlingCenterId");
        new[] { count, exceeds, center }.ShouldAllBe(e => !e.Metadata!.ContainsKey(SeriesErrors.StageOrderKey));
    }

    [Fact(DisplayName = "Locked should be a Conflict and NotFound should be NotFound")]
    public void LockedAndNotFound_ShouldHaveExpectedTypes()
    {
        // Arrange & Act & Assert
        SeriesErrors.Locked.Type.ShouldBe(ErrorType.Conflict);
        SeriesErrors.NotFound(SeriesId.New()).Type.ShouldBe(ErrorType.NotFound);
        SeriesErrors.LogoChangeDenied.Type.ShouldBe(ErrorType.Forbidden);
    }
}