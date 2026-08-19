using Neba.Api.Legacy.Tournaments.Stats;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy.Tournaments.Stats;

[UnitTest]
[Component("Legacy")]
public sealed class UpdateTournamentStatsRequestValidatorTests
{
    private readonly UpdateTournamentStatsRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when TournamentId is greater than zero")]
    public void Validate_ShouldSucceed_WhenTournamentIdIsGreaterThanZero()
    {
        // Arrange
        var request = new UpdateTournamentStatsRequest(1);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail when TournamentId is not greater than zero")]
    [InlineData(0, TestDisplayName = "Zero")]
    [InlineData(-1, TestDisplayName = "Negative")]
    public void Validate_ShouldFail_WhenTournamentIdIsNotGreaterThanZero(int tournamentId)
    {
        // Arrange
        var request = new UpdateTournamentStatsRequest(tournamentId);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateTournamentStatsRequest.TournamentId));
    }
}