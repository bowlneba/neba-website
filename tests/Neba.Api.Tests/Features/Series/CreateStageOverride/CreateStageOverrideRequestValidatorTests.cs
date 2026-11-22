using Neba.Api.Contracts.Series;
using Neba.Api.Contracts.Series.CreateStageOverride;
using Neba.Api.Features.Series.CreateStageOverride;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.CreateStageOverride;

[UnitTest]
[Component("Series")]
public sealed class CreateStageOverrideRequestValidatorTests
{
    private readonly CreateStageOverrideRequestValidator _validator = new();

    private static CreateStageOverrideRequest Request(
        string? id = null, string? stageId = null, string? bowlerId = null, int? kind = null, string? reason = null, int? version = null)
        => CreateStageOverrideRequestFactory.Create(
            id: id ?? new string('0', 26),
            stageId: stageId ?? new string('1', 26),
            stageOverride: CreateStageOverrideInputFactory.Create(
                kind: kind, bowlerId: bowlerId ?? new string('2', 26), reason: reason, version: version));

    [Theory(DisplayName = "Validate should pass for an Add or a Clear with valid ids, a reason and a Version of zero or more")]
    [InlineData(OverrideKindValues.Add)]
    [InlineData(OverrideKindValues.Clear)]
    public async Task Validate_ShouldPass_ForValidRequest(int kind)
    {
        // Arrange
        var request = Request(kind: kind, version: 0);

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail with Required for an empty id")]
    [InlineData("Id")]
    [InlineData("StageId")]
    [InlineData("BowlerId")]
    public async Task Validate_ShouldFail_ForEmptyId(string field)
    {
        // Arrange
        var request = field switch
        {
            "Id" => Request(id: string.Empty),
            "StageId" => Request(stageId: string.Empty),
            _ => Request(bowlerId: string.Empty),
        };

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == $"CreateStageOverrideRequest.{field}Required");
    }

    [Theory(DisplayName = "Validate should fail with InvalidLength for an id that is not 26 characters")]
    [InlineData("Id", 25)]
    [InlineData("Id", 27)]
    [InlineData("StageId", 25)]
    [InlineData("StageId", 27)]
    [InlineData("BowlerId", 25)]
    [InlineData("BowlerId", 27)]
    public async Task Validate_ShouldFail_ForWrongLengthId(string field, int length)
    {
        // Arrange
        var value = new string('0', length);
        var request = field switch
        {
            "Id" => Request(id: value),
            "StageId" => Request(stageId: value),
            _ => Request(bowlerId: value),
        };

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Single().ErrorCode.ShouldBe($"CreateStageOverrideRequest.{field}InvalidLength");
    }

    [Theory(DisplayName = "Validate should fail with KindInvalid for a Kind that is not 1 or 2")]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-1)]
    public async Task Validate_ShouldFail_ForKindOutsideAddAndClear(int kind)
    {
        // Arrange
        var request = Request(kind: kind);

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Single().ErrorCode.ShouldBe("CreateStageOverrideRequest.KindInvalid");
    }

    [Theory(DisplayName = "Validate should fail with ReasonRequired for an empty or blank reason")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_ShouldFail_ForBlankReason(string reason)
    {
        // Arrange
        var request = Request(reason: reason);

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Single().ErrorCode.ShouldBe("CreateStageOverrideRequest.ReasonRequired");
    }

    [Fact(DisplayName = "Validate should pass for a reason of exactly 500 characters")]
    public async Task Validate_ShouldPass_ForReasonAtTheLimit()
    {
        // Arrange
        var request = Request(reason: new string('r', 500));

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail with ReasonTooLong for a reason of 501 characters")]
    public async Task Validate_ShouldFail_ForReasonOverTheLimit()
    {
        // Arrange
        var request = Request(reason: new string('r', 501));

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Single().ErrorCode.ShouldBe("CreateStageOverrideRequest.ReasonTooLong");
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
        result.Errors.Single().ErrorCode.ShouldBe("CreateStageOverrideRequest.VersionInvalid");
    }
}