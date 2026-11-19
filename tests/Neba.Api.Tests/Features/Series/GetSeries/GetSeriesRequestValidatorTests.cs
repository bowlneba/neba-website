using Neba.Api.Features.Series.GetSeries;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.GetSeries;

[UnitTest]
[Component("Series")]
public sealed class GetSeriesRequestValidatorTests
{
    private readonly GetSeriesRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should pass for a 26-character id")]
    public async Task Validate_ShouldPass_ForValidId()
    {
        // Arrange
        var request = new GetSeriesRequest { SeriesId = new string('0', 26) };

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail with SeriesIdRequired for an empty id")]
    public async Task Validate_ShouldFail_ForEmptyId()
    {
        // Arrange
        var request = new GetSeriesRequest { SeriesId = string.Empty };

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldContain(e => e.ErrorCode == "GetSeriesRequest.SeriesIdRequired");
    }

    [Fact(DisplayName = "Validate should fail with SeriesIdInvalidLength for a wrong-length id")]
    public async Task Validate_ShouldFail_ForWrongLengthId()
    {
        // Arrange
        var request = new GetSeriesRequest { SeriesId = "short" };

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldContain(e => e.ErrorCode == "GetSeriesRequest.SeriesIdInvalidLength");
    }
}