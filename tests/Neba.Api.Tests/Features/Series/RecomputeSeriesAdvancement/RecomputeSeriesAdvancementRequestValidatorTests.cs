using Neba.Api.Contracts.Series.RecomputeSeriesAdvancement;
using Neba.Api.Features.Series.RecomputeSeriesAdvancement;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.RecomputeSeriesAdvancement;

[UnitTest]
[Component("Series")]
public sealed class RecomputeSeriesAdvancementRequestValidatorTests
{
    private readonly RecomputeSeriesAdvancementRequestValidator _validator = new();

    private static RecomputeSeriesAdvancementRequest Request(string? id = null, string? stageId = null, int? version = null)
        => RecomputeSeriesAdvancementRequestFactory.Create(
            id: id ?? new string('0', 26),
            stageId: stageId ?? new string('1', 26),
            recomputation: RecomputeSeriesAdvancementInputFactory.Create(version: version));

    [Fact(DisplayName = "Validate should pass for 26-character ids and a Version of zero or more")]
    public async Task Validate_ShouldPass_ForValidRequest()
    {
        // Arrange
        var request = Request(version: 0);

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail with Required for an empty Series id or Stage id")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_ShouldFail_ForEmptyId(bool seriesId)
    {
        // Arrange
        var request = seriesId ? Request(id: string.Empty) : Request(stageId: string.Empty);
        var code = seriesId ? "RecomputeSeriesAdvancementRequest.IdRequired" : "RecomputeSeriesAdvancementRequest.StageIdRequired";

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == code);
    }

    [Theory(DisplayName = "Validate should fail with InvalidLength for a Series id or Stage id that is not 26 characters")]
    [InlineData(25, true)]
    [InlineData(27, true)]
    [InlineData(25, false)]
    [InlineData(27, false)]
    public async Task Validate_ShouldFail_ForWrongLengthId(int length, bool seriesId)
    {
        // Arrange
        var value = new string('0', length);
        var request = seriesId ? Request(id: value) : Request(stageId: value);
        var code = seriesId ? "RecomputeSeriesAdvancementRequest.IdInvalidLength" : "RecomputeSeriesAdvancementRequest.StageIdInvalidLength";

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Single().ErrorCode.ShouldBe(code);
    }

    [Fact(DisplayName = "Validate should fail with VersionInvalid for a negative Version")]
    public async Task Validate_ShouldFail_ForNegativeVersion()
    {
        // Arrange
        var request = Request(version: -1);

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Single().ErrorCode.ShouldBe("RecomputeSeriesAdvancementRequest.VersionInvalid");
    }
}