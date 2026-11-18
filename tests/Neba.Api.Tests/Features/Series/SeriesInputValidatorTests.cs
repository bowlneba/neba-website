using Neba.Api.Features.Series;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesInputValidatorTests
{
    private readonly SeriesStageInputValidator _stage = new();
    private readonly SeriesFinaleInputValidator _finale = new();

    [Fact(DisplayName = "Stage validator should pass for a valid Qualifying Stage")]
    public async Task Stage_ShouldPass_ForValidStage()
    {
        // Arrange
        var input = SeriesStageInputFactory.Create(tournamentId: new string('1', 26));

        // Act
        var result = await _stage.ValidateAsync(input, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Stage validator should fail for an id that is not 26 characters")]
    public async Task Stage_ShouldFail_ForBadStageId()
    {
        // Arrange
        var input = SeriesStageInputFactory.Create(id: "short", tournamentId: new string('1', 26));

        // Act
        var result = await _stage.ValidateAsync(input, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldContain(e => e.ErrorCode == "SeriesStageInput.IdInvalidLength");
    }

    [Fact(DisplayName = "Stage validator should fail for an unknown kind")]
    public async Task Stage_ShouldFail_ForUnknownKind()
    {
        // Arrange
        var input = SeriesStageInputFactory.Create(kind: 99, tournamentId: new string('1', 26));

        // Act
        var result = await _stage.ValidateAsync(input, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldContain(e => e.ErrorCode == "SeriesStageInput.KindInvalid");
    }

    [Fact(DisplayName = "Stage validator should fail for a missing or short tournament id")]
    public async Task Stage_ShouldFail_ForBadTournamentId()
    {
        // Arrange
        var input = SeriesStageInputFactory.Create(tournamentId: "short");

        // Act
        var result = await _stage.ValidateAsync(input, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldContain(e => e.ErrorCode == "SeriesStageInput.TournamentIdInvalid");
    }

    [Theory(DisplayName = "Stage validator should fail when only one of squad date and start time is given")]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Stage_ShouldFail_ForIncompleteSquad(bool withDate, bool withTime)
    {
        // Arrange
        var input = SeriesStageInputFactory.Create(
            tournamentId: new string('1', 26),
            squadDate: withDate ? new DateOnly(2025, 7, 12) : null,
            squadStartTime: withTime ? new TimeOnly(9, 0) : null);

        // Act
        var result = await _stage.ValidateAsync(input, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldContain(e => e.ErrorCode == "SeriesStageInput.SquadIncomplete");
    }

    [Fact(DisplayName = "Finale validator should pass for a Finale with a date and start time")]
    public async Task Finale_ShouldPass_ForValidFinale()
    {
        // Arrange
        var input = SeriesFinaleInputFactory.Create();

        // Act
        var result = await _finale.ValidateAsync(input, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Finale validator should fail for a missing date")]
    public async Task Finale_ShouldFail_ForMissingDate()
    {
        // Arrange
        var input = new Neba.Api.Contracts.Series.CreateSeries.SeriesFinaleInput { FinalistCount = 4, StartTime = new TimeOnly(14, 0) };

        // Act
        var result = await _finale.ValidateAsync(input, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldContain(e => e.ErrorCode == "SeriesFinaleInput.DateRequired");
    }

    [Fact(DisplayName = "Finale validator should fail for a missing start time")]
    public async Task Finale_ShouldFail_ForMissingStartTime()
    {
        // Arrange
        var input = new Neba.Api.Contracts.Series.CreateSeries.SeriesFinaleInput { FinalistCount = 4, Date = new DateOnly(2025, 9, 27) };

        // Act
        var result = await _finale.ValidateAsync(input, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldContain(e => e.ErrorCode == "SeriesFinaleInput.StartTimeRequired");
    }

    [Fact(DisplayName = "Finale validator should fail for a center id longer than 6 characters, and ignore a blank one")]
    public async Task Finale_ShouldValidateCenterId()
    {
        // Arrange
        var tooLong = SeriesFinaleInputFactory.Create(bowlingCenterId: "1234567");
        var blank = SeriesFinaleInputFactory.Create(bowlingCenterId: "   ");

        // Act
        var longResult = await _finale.ValidateAsync(tooLong, TestContext.Current.CancellationToken);
        var blankResult = await _finale.ValidateAsync(blank, TestContext.Current.CancellationToken);

        // Assert
        longResult.Errors.ShouldContain(e => e.ErrorCode == "SeriesFinaleInput.BowlingCenterIdInvalid");
        blankResult.IsValid.ShouldBeTrue();
    }
}