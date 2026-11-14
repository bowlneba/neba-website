using Neba.Api.Features.Tournaments.CheckTeamEligibility;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.CheckTeamEligibility;

[UnitTest]
[Component("Tournaments")]
public sealed class CheckTeamEligibilityRequestValidatorTests
{
    private const string ValidId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E";
    private const string OtherValidId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2F";

    private readonly CheckTeamEligibilityRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when the request is valid")]
    public void Validate_ShouldSucceed_WhenRequestIsValid()
    {
        // Arrange
        var request = new CheckTeamEligibilityRequest { Id = ValidId, BowlerIds = [ValidId, OtherValidId], UnknownBowlerCount = 0 };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail with IdRequired when the tournament ID is empty")]
    [InlineData("", TestDisplayName = "Empty")]
    [InlineData("   ", TestDisplayName = "Whitespace")]
    public void Validate_ShouldFailWithIdRequired_WhenIdIsEmpty(string id)
    {
        // Arrange
        var request = new CheckTeamEligibilityRequest { Id = id };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.ShouldContain(error => error.ErrorCode == "CheckTeamEligibilityRequest.IdRequired");
    }

    [Fact(DisplayName = "Validate should fail with IdInvalidLength when the tournament ID is not 26 characters")]
    public void Validate_ShouldFailWithIdInvalidLength_WhenIdIsWrongLength()
    {
        // Arrange
        var request = new CheckTeamEligibilityRequest { Id = "short" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.ShouldContain(error => error.ErrorCode == "CheckTeamEligibilityRequest.IdInvalidLength");
    }

    [Fact(DisplayName = "Validate should fail with BowlerIdInvalidLength when a bowler ID is not 26 characters")]
    public void Validate_ShouldFailWithBowlerIdInvalidLength_WhenABowlerIdIsWrongLength()
    {
        // Arrange
        var request = new CheckTeamEligibilityRequest { Id = ValidId, BowlerIds = [ValidId, "short"] };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.ShouldContain(error => error.ErrorCode == "CheckTeamEligibilityRequest.BowlerIdInvalidLength");
    }

    [Fact(DisplayName = "Validate should fail with BowlerIdsDuplicate when a bowler appears twice")]
    public void Validate_ShouldFailWithBowlerIdsDuplicate_WhenABowlerAppearsTwice()
    {
        // Arrange
        var request = new CheckTeamEligibilityRequest { Id = ValidId, BowlerIds = [ValidId, ValidId] };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.ShouldContain(error => error.ErrorCode == "CheckTeamEligibilityRequest.BowlerIdsDuplicate");
    }

    [Fact(DisplayName = "Validate should fail with TooManyBowlerIds when more than five bowler IDs are given")]
    public void Validate_ShouldFailWithTooManyBowlerIds_WhenMoreThanFive()
    {
        // Arrange
        var ids = Enumerable.Range(0, 6).Select(index => "01J7ZK8X6ZQJ8V3F8N9T9C9R2" + index.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var request = new CheckTeamEligibilityRequest { Id = ValidId, BowlerIds = ids };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.ShouldContain(error => error.ErrorCode == "CheckTeamEligibilityRequest.TooManyBowlerIds");
    }

    [Theory(DisplayName = "Validate should fail with UnknownBowlerCountInvalid when the count is outside 0 to 5")]
    [InlineData(-1, TestDisplayName = "Negative")]
    [InlineData(6, TestDisplayName = "Six")]
    public void Validate_ShouldFailWithUnknownBowlerCountInvalid_WhenCountIsOutOfRange(int count)
    {
        // Arrange
        var request = new CheckTeamEligibilityRequest { Id = ValidId, UnknownBowlerCount = count };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.ShouldContain(error => error.ErrorCode == "CheckTeamEligibilityRequest.UnknownBowlerCountInvalid");
    }
}