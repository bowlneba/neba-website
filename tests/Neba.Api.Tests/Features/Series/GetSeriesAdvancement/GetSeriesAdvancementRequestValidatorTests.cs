using Neba.Api.Features.Series.GetSeriesAdvancement;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.GetSeriesAdvancement;

[UnitTest]
[Component("Series")]
public sealed class GetSeriesAdvancementRequestValidatorTests
{
    private readonly GetSeriesAdvancementRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should pass for a 26-character id")]
    public async Task Validate_ShouldPass_ForValidId()
    {
        // Arrange
        var request = new GetSeriesAdvancementRequest { SeriesId = new string('0', 26) };

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail with SeriesIdRequired for an empty id")]
    public async Task Validate_ShouldFail_ForEmptyId()
    {
        // Arrange
        var request = new GetSeriesAdvancementRequest { SeriesId = string.Empty };

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "GetSeriesAdvancementRequest.SeriesIdRequired");
    }

    [Theory(DisplayName = "Validate should fail with SeriesIdInvalidLength for an id that is not 26 characters")]
    [InlineData(25)]
    [InlineData(27)]
    public async Task Validate_ShouldFail_ForWrongLengthId(int length)
    {
        // Arrange
        var request = new GetSeriesAdvancementRequest { SeriesId = new string('0', length) };

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "GetSeriesAdvancementRequest.SeriesIdInvalidLength");
        result.Errors.ShouldNotContain(e => e.ErrorCode == "GetSeriesAdvancementRequest.SeriesIdRequired");
    }
}