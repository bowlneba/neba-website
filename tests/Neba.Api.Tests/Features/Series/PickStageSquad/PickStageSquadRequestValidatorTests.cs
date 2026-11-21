using Neba.Api.Contracts.Series.PickStageSquad;
using Neba.Api.Features.Series.PickStageSquad;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.PickStageSquad;

[UnitTest]
[Component("Series")]
public sealed class PickStageSquadRequestValidatorTests
{
    private readonly PickStageSquadRequestValidator _validator = new();

    private static PickStageSquadRequest Request(string? id = null, string? stageId = null, string? squadId = null, int? version = null)
        => PickStageSquadRequestFactory.Create(
            id: id ?? new string('0', 26),
            stageId: stageId ?? new string('1', 26),
            squad: PickStageSquadInputFactory.Create(squadId: squadId ?? new string('2', 26), version: version));

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
    [InlineData("SquadId")]
    public async Task Validate_ShouldFail_ForEmptyId(string field)
    {
        // Arrange
        var request = field switch
        {
            "Id" => Request(id: string.Empty),
            "StageId" => Request(stageId: string.Empty),
            _ => Request(squadId: string.Empty),
        };

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == $"PickStageSquadRequest.{field}Required");
    }

    [Theory(DisplayName = "Validate should fail with InvalidLength for an id that is not 26 characters")]
    [InlineData("Id", 25)]
    [InlineData("Id", 27)]
    [InlineData("StageId", 25)]
    [InlineData("StageId", 27)]
    [InlineData("SquadId", 25)]
    [InlineData("SquadId", 27)]
    public async Task Validate_ShouldFail_ForWrongLengthId(string field, int length)
    {
        // Arrange
        var value = new string('0', length);
        var request = field switch
        {
            "Id" => Request(id: value),
            "StageId" => Request(stageId: value),
            _ => Request(squadId: value),
        };

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Single().ErrorCode.ShouldBe($"PickStageSquadRequest.{field}InvalidLength");
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
        result.Errors.Single().ErrorCode.ShouldBe("PickStageSquadRequest.VersionInvalid");
    }
}