using Neba.Api.Features.Series.ListStageResults;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.ListStageResults;

[UnitTest]
[Component("Series")]
public sealed class ListStageResultsRequestValidatorTests
{
    private static readonly string ValidId = new('0', 26);

    private readonly ListStageResultsRequestValidator _validator = new();

    private static ListStageResultsRequest Request(string? seriesId = null, string? stageId = null)
        => new() { SeriesId = seriesId ?? ValidId, StageId = stageId ?? ValidId };

    [Fact(DisplayName = "Validate should pass when both ids are 26 characters")]
    public async Task Validate_ShouldPass_ForValidIds()
    {
        // Arrange
        var request = Request();

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail with SeriesIdRequired for an empty Series id")]
    public async Task Validate_ShouldFail_ForEmptySeriesId()
    {
        // Arrange
        var request = Request(seriesId: string.Empty);

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "ListStageResultsRequest.SeriesIdRequired");
        result.Errors.ShouldNotContain(e => e.ErrorCode.StartsWith("ListStageResultsRequest.StageId", StringComparison.Ordinal));
    }

    [Theory(DisplayName = "Validate should fail with SeriesIdInvalidLength for a Series id that is not 26 characters")]
    [InlineData(25)]
    [InlineData(27)]
    public async Task Validate_ShouldFail_ForWrongLengthSeriesId(int length)
    {
        // Arrange
        var request = Request(seriesId: new string('0', length));

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "ListStageResultsRequest.SeriesIdInvalidLength");
        result.Errors.ShouldNotContain(e => e.ErrorCode.StartsWith("ListStageResultsRequest.StageId", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Validate should fail with StageIdRequired for an empty Stage id")]
    public async Task Validate_ShouldFail_ForEmptyStageId()
    {
        // Arrange
        var request = Request(stageId: string.Empty);

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "ListStageResultsRequest.StageIdRequired");
        result.Errors.ShouldNotContain(e => e.ErrorCode.StartsWith("ListStageResultsRequest.SeriesId", StringComparison.Ordinal));
    }

    [Theory(DisplayName = "Validate should fail with StageIdInvalidLength for a Stage id that is not 26 characters")]
    [InlineData(25)]
    [InlineData(27)]
    public async Task Validate_ShouldFail_ForWrongLengthStageId(int length)
    {
        // Arrange
        var request = Request(stageId: new string('0', length));

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "ListStageResultsRequest.StageIdInvalidLength");
        result.Errors.ShouldNotContain(e => e.ErrorCode.StartsWith("ListStageResultsRequest.SeriesId", StringComparison.Ordinal));
    }
}