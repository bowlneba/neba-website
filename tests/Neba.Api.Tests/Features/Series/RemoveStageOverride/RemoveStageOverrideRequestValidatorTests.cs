using Neba.Api.Features.Series.RemoveStageOverride;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.RemoveStageOverride;

[UnitTest]
[Component("Series")]
public sealed class RemoveStageOverrideRequestValidatorTests
{
    private readonly RemoveStageOverrideRequestValidator _validator = new();

    private static RemoveStageOverrideRequest Request(
        string? id = null, string? stageId = null, string? overrideId = null, int? version = null, bool omitVersion = false)
        => RemoveStageOverrideRequestFactory.Create(
            id: id ?? new string('0', 26),
            stageId: stageId ?? new string('1', 26),
            overrideId: overrideId ?? new string('2', 26),
            version: version,
            omitVersion: omitVersion);

    [Fact(DisplayName = "Validate should pass for 26-character ids and a Version of zero")]
    public async Task Validate_ShouldPass_ForValidRequest()
    {
        // Arrange
        var request = Request(version: 0);

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail with Required for an empty id")]
    [InlineData("Id")]
    [InlineData("StageId")]
    [InlineData("OverrideId")]
    public async Task Validate_ShouldFail_ForEmptyId(string field)
    {
        // Arrange
        var request = field switch
        {
            "Id" => Request(id: string.Empty),
            "StageId" => Request(stageId: string.Empty),
            _ => Request(overrideId: string.Empty),
        };

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == $"RemoveStageOverrideRequest.{field}Required");
    }

    [Theory(DisplayName = "Validate should fail with InvalidLength for an id that is not 26 characters")]
    [InlineData("Id", 25)]
    [InlineData("Id", 27)]
    [InlineData("StageId", 25)]
    [InlineData("StageId", 27)]
    [InlineData("OverrideId", 25)]
    [InlineData("OverrideId", 27)]
    public async Task Validate_ShouldFail_ForWrongLengthId(string field, int length)
    {
        // Arrange
        var value = new string('0', length);
        var request = field switch
        {
            "Id" => Request(id: value),
            "StageId" => Request(stageId: value),
            _ => Request(overrideId: value),
        };

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Single().ErrorCode.ShouldBe($"RemoveStageOverrideRequest.{field}InvalidLength");
    }

    [Fact(DisplayName = "Validate should fail with VersionRequired when the version is missing")]
    public async Task Validate_ShouldFail_WhenVersionIsMissing()
    {
        // Arrange
        var request = Request(omitVersion: true);

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Single().ErrorCode.ShouldBe("RemoveStageOverrideRequest.VersionRequired");
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
        result.Errors.Single().ErrorCode.ShouldBe("RemoveStageOverrideRequest.VersionInvalid");
    }
}