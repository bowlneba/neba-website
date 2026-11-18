using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Features.Series.CreateSeries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.CreateSeries;

[UnitTest]
[Component("Series")]
public sealed class CreateSeriesRequestValidatorTests
{
    private readonly CreateSeriesRequestValidator _validator = new();

    private static CreateSeriesRequest Request(SeriesInput? input = null)
        => new() { Series = input ?? SeriesInputFactory.Create(seasonId: new string('2', 26), stages: [SeriesStageInputFactory.Create(tournamentId: new string('1', 26))]) };

    [Fact(DisplayName = "Validate should pass for a valid request")]
    public async Task Validate_ShouldPass_ForValidRequest()
    {
        // Arrange & Act
        var result = await _validator.ValidateAsync(Request(), TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail for an empty name")]
    public async Task Validate_ShouldFail_ForEmptyName()
    {
        // Arrange
        var request = Request(SeriesInputFactory.Create(name: "", seasonId: new string('2', 26)));

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateSeriesRequest.NameRequired");
    }

    [Fact(DisplayName = "Validate should fail for a name over 127 characters")]
    public async Task Validate_ShouldFail_ForLongName()
    {
        // Arrange
        var request = Request(SeriesInputFactory.Create(name: new string('x', 128), seasonId: new string('2', 26)));

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateSeriesRequest.NameTooLong");
    }

    [Fact(DisplayName = "Validate should fail for a bad season id")]
    public async Task Validate_ShouldFail_ForBadSeasonId()
    {
        // Arrange
        var request = Request(SeriesInputFactory.Create(seasonId: "short"));

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateSeriesRequest.SeasonIdInvalid");
    }

    [Fact(DisplayName = "Validate should run the Stage and Finale validators")]
    public async Task Validate_ShouldRunNestedValidators()
    {
        // Arrange
        var input = SeriesInputFactory.Create(
            seasonId: new string('2', 26),
            stages: [SeriesStageInputFactory.Create(kind: "Nope", tournamentId: new string('1', 26))],
            finale: new SeriesFinaleInput { FinalistCount = 4 });

        // Act
        var result = await _validator.ValidateAsync(Request(input), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Select(e => e.ErrorCode).ShouldContain("SeriesStageInput.KindInvalid");
        result.Errors.Select(e => e.ErrorCode).ShouldContain("SeriesFinaleInput.DateRequired");
    }
}