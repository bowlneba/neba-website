using Neba.Api.Legacy.Seasons.Complete;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy.Seasons.Complete;

[UnitTest]
[Component("Legacy")]
public sealed class CompleteSeasonRequestValidatorTests
{
    private readonly CompleteSeasonRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when SeasonId is greater than zero")]
    public void Validate_ShouldSucceed_WhenSeasonIdIsGreaterThanZero()
    {
        // Arrange
        var request = new CompleteSeasonRequest(1);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail when SeasonId is not greater than zero")]
    [InlineData(0, TestDisplayName = "Zero")]
    [InlineData(-1, TestDisplayName = "Negative")]
    public void Validate_ShouldFail_WhenSeasonIdIsNotGreaterThanZero(int seasonId)
    {
        // Arrange
        var request = new CompleteSeasonRequest(seasonId);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CompleteSeasonRequest.SeasonId));
    }
}