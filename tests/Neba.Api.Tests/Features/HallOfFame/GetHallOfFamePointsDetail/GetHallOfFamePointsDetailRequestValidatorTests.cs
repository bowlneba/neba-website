using Neba.Api.Features.HallOfFame.GetHallOfFamePointsDetail;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.HallOfFame.GetHallOfFamePointsDetail;

[UnitTest]
[Component("HallOfFame")]
public sealed class GetHallOfFamePointsDetailRequestValidatorTests
{
    private readonly GetHallOfFamePointsDetailRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should pass for a valid ULID")]
    public void Validate_ShouldPass_ForValidUlid()
    {
        // Arrange
        var request = new GetHallOfFamePointsDetailRequest { BowlerId = "01JZ8K3M4N5P6Q7R8S9T0V1W2X" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should return the required error when the bowler ID is empty")]
    [InlineData("", TestDisplayName = "empty string")]
    [InlineData("   ", TestDisplayName = "whitespace only")]
    public void Validate_ShouldReturnRequiredError_WhenEmpty(string bowlerId)
    {
        // Arrange
        var request = new GetHallOfFamePointsDetailRequest { BowlerId = bowlerId };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorCode == "GetHallOfFamePointsDetailRequest.BowlerIdRequired");
    }

    [Fact(DisplayName = "Validate should return the required error when the bowler ID is null")]
    public void Validate_ShouldReturnRequiredError_WhenNull()
    {
#nullable disable
        // Arrange
        var request = new GetHallOfFamePointsDetailRequest { BowlerId = null };

        // Act
        var result = _validator.Validate(request);
#nullable enable

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorCode == "GetHallOfFamePointsDetailRequest.BowlerIdRequired");
    }

    [Fact(DisplayName = "Validate should return the length error when the bowler ID is not 26 characters")]
    public void Validate_ShouldReturnLengthError_WhenNot26Characters()
    {
        // Arrange
        var request = new GetHallOfFamePointsDetailRequest { BowlerId = "01JZ8K3M" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorCode == "GetHallOfFamePointsDetailRequest.BowlerIdInvalidLength");
    }
}
