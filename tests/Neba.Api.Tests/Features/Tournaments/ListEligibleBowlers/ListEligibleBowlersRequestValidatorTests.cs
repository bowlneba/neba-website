using Neba.Api.Features.Tournaments.ListEligibleBowlers;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.ListEligibleBowlers;

[UnitTest]
[Component("Tournaments")]
public sealed class ListEligibleBowlersRequestValidatorTests
{
    private const string ValidId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E";

    private readonly ListEligibleBowlersRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when the tournament ID is 26 characters")]
    public void Validate_ShouldSucceed_WhenIdIsValid()
    {
        // Arrange
        var request = new ListEligibleBowlersRequest { TournamentId = ValidId };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail with TournamentIdRequired when the ID is empty")]
    [InlineData("", TestDisplayName = "Empty")]
    [InlineData("   ", TestDisplayName = "Whitespace")]
    public void Validate_ShouldFailWithRequired_WhenIdIsEmpty(string id)
    {
        // Arrange
        var request = new ListEligibleBowlersRequest { TournamentId = id };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.ShouldContain(error => error.ErrorCode == "ListEligibleBowlersRequest.TournamentIdRequired");
    }

    [Theory(DisplayName = "Validate should fail with TournamentIdInvalidLength when the ID is not 26 characters")]
    [InlineData(25, TestDisplayName = "25 characters")]
    [InlineData(27, TestDisplayName = "27 characters")]
    public void Validate_ShouldFailWithInvalidLength_WhenIdIsWrongLength(int length)
    {
        // Arrange
        var request = new ListEligibleBowlersRequest { TournamentId = new string('A', length) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.ShouldContain(error => error.ErrorCode == "ListEligibleBowlersRequest.TournamentIdInvalidLength");
    }
}