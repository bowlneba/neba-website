using Neba.Api.Legacy.Tournaments.Complete;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy.Tournaments.Complete;

[UnitTest]
[Component("Legacy")]
public sealed class UnsyncedBowlerResultSyncEmailTests
{
    [Fact(DisplayName = "ToHtmlBody should include the legacy tournament id and every unmapped bowler id")]
    public void ToHtmlBody_ShouldIncludeLegacyTournamentIdAndEveryUnmappedBowlerId()
    {
        // Arrange
        var email = new UnsyncedBowlerResultSyncEmail(
            legacyBowlerIds: [42, 43],
            legacyTournamentId: 7,
            isTeamTournament: false);

        // Act
        var body = email.ToHtmlBody();

        // Assert
        body.ShouldContain("42");
        body.ShouldContain("43");
        body.ShouldContain("7");
    }

    [Fact(DisplayName = "ToHtmlBody should include the team-tournament caveat when isTeamTournament is true")]
    public void ToHtmlBody_ShouldIncludeTeamCaveat_WhenIsTeamTournamentIsTrue()
    {
        // Arrange
        var email = new UnsyncedBowlerResultSyncEmail([42], legacyTournamentId: 7, isTeamTournament: true);

        // Act
        var body = email.ToHtmlBody();

        // Assert
        body.ShouldContain("team tournament", Case.Insensitive);
    }

    [Fact(DisplayName = "ToHtmlBody should omit the team-tournament caveat when isTeamTournament is false")]
    public void ToHtmlBody_ShouldOmitTeamCaveat_WhenIsTeamTournamentIsFalse()
    {
        // Arrange
        var email = new UnsyncedBowlerResultSyncEmail([42], legacyTournamentId: 7, isTeamTournament: false);

        // Act
        var body = email.ToHtmlBody();

        // Assert
        body.ShouldNotContain("team tournament", Case.Insensitive);
    }
}