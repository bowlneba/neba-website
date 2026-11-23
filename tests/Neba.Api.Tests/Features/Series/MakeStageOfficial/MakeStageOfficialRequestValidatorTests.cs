using Neba.Api.Contracts.Series.MakeStageOfficial;
using Neba.Api.Features.Series.MakeStageOfficial;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.MakeStageOfficial;

[UnitTest]
[Component("Series")]
public sealed class MakeStageOfficialRequestValidatorTests
{
    private readonly MakeStageOfficialRequestValidator _validator = new();

    private static MakeStageOfficialRequest Request(string? id = null, string? stageId = null, int? version = null)
        => MakeStageOfficialRequestFactory.Create(
            id: id ?? new string('0', 26),
            stageId: stageId ?? new string('1', 26),
            official: MakeStageOfficialInputFactory.Create(version));

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

    [Theory(DisplayName = "Validate should fail with Required for an empty id")]
    [InlineData("Id")]
    [InlineData("StageId")]
    public async Task Validate_ShouldFail_ForEmptyId(string field)
    {
        // Arrange
        var request = field == "Id" ? Request(id: string.Empty) : Request(stageId: string.Empty);

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == $"MakeStageOfficialRequest.{field}Required");
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
        result.Errors.ShouldContain(e => e.ErrorCode == "MakeStageOfficialRequest.VersionInvalid");
    }
}