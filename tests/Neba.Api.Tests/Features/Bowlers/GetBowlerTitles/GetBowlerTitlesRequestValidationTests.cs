using Neba.Api.Features.Bowlers.GetBowlerTitles;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Bowlers.GetBowlerTitles;

[UnitTest]
[Component("Bowlers")]
public sealed class GetBowlerTitlesRequestValidationTests
{
    [Fact(DisplayName = "Validate should return bowler ID required error when bowler ID is null")]
    public void Validate_ShouldReturnBowlerIdRequiredError_WhenBowlerIdIsNull()
    {
#nullable disable
        // Arrange
        var validator = new GetBowlerTitlesRequestValidator();
        var request = new GetBowlerTitlesRequest { BowlerId = null };

        // Act
        var result = validator.Validate(request);
#nullable enable

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error =>
            error.PropertyName == nameof(GetBowlerTitlesRequest.BowlerId)
            && error.ErrorCode == "GetBowlerTitlesRequest.BowlerIdRequired"
            && error.ErrorMessage == "Bowler ID is required.");
    }

    [Theory(DisplayName = "Validate should return bowler ID required error when bowler ID is empty")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldReturnBowlerIdRequiredError_WhenBowlerIdIsEmpty(string bowlerId)
    {
        // Arrange
        var validator = new GetBowlerTitlesRequestValidator();
        var request = new GetBowlerTitlesRequest { BowlerId = bowlerId };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error =>
            error.PropertyName == nameof(GetBowlerTitlesRequest.BowlerId)
            && error.ErrorCode == "GetBowlerTitlesRequest.BowlerIdRequired"
            && error.ErrorMessage == "Bowler ID is required.");
    }

    [Theory(DisplayName = "Validate should return invalid length error when bowler ID is not 26 characters")]
    [InlineData("tooshort")]
    [InlineData("thisstringiswaytoolongtobeavalidulid")]
    public void Validate_ShouldReturnInvalidLengthError_WhenBowlerIdIsNotTwentySixCharacters(string bowlerId)
    {
        // Arrange
        var validator = new GetBowlerTitlesRequestValidator();
        var request = new GetBowlerTitlesRequest { BowlerId = bowlerId };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error =>
            error.PropertyName == nameof(GetBowlerTitlesRequest.BowlerId)
            && error.ErrorCode == "GetBowlerTitlesRequest.BowlerIdInvalidLength"
            && error.ErrorMessage == "Bowler ID must be a 26-character ULID.");
    }

    [Fact(DisplayName = "Validate should succeed when bowler ID is a valid 26-character ULID")]
    public void Validate_ShouldSucceed_WhenBowlerIdIsValidUlid()
    {
        // Arrange
        var validator = new GetBowlerTitlesRequestValidator();
        var request = new GetBowlerTitlesRequest { BowlerId = "01000000000000000000000001" };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}
