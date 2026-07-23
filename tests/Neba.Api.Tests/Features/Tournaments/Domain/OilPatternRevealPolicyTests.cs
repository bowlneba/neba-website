using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[UnitTest]
[Component("Tournaments")]
public sealed class OilPatternRevealPolicyTests
{
    [Fact(DisplayName = "IsRevealed returns true when there is no reveal date")]
    public void IsRevealed_ShouldReturnTrue_WhenRevealDateTimeIsNull()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        // Act
        var result = OilPatternRevealPolicy.IsRevealed(null, callerHasTournamentManagementPermission: false, now);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsRevealed returns true when the caller holds the tournament management permission, regardless of the reveal date")]
    public void IsRevealed_ShouldReturnTrue_WhenCallerHasTournamentManagementPermission()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var futureRevealDate = now.AddDays(7);

        // Act
        var result = OilPatternRevealPolicy.IsRevealed(futureRevealDate, callerHasTournamentManagementPermission: true, now);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsRevealed returns false when the reveal date is in the future and the caller lacks the management permission")]
    public void IsRevealed_ShouldReturnFalse_WhenRevealDateTimeIsInFutureAndCallerLacksManagementPermission()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var futureRevealDate = now.AddDays(7);

        // Act — covers both the anonymous caller and the authenticated-but-non-management caller; the
        // policy takes no authentication flag at all, so both shapes reduce to the same boolean input.
        var result = OilPatternRevealPolicy.IsRevealed(futureRevealDate, callerHasTournamentManagementPermission: false, now);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsRevealed returns true when the reveal date has already passed and the caller lacks the management permission")]
    public void IsRevealed_ShouldReturnTrue_WhenRevealDateTimeHasPassedAndCallerLacksManagementPermission()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var pastRevealDate = now.AddDays(-7);

        // Act
        var result = OilPatternRevealPolicy.IsRevealed(pastRevealDate, callerHasTournamentManagementPermission: false, now);

        // Assert
        result.ShouldBeTrue();
    }
}
