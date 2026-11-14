using ErrorOr;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.CheckTeamEligibility;

internal static class TeamEligibilityErrors
{
    public static Error NotATeamTournament(TournamentId tournamentId)
        => Error.Validation(
            code: "TeamEligibility.NotATeamTournament",
            description: "This tournament is not a team event.",
            metadata: new Dictionary<string, object> { { "TournamentId", tournamentId.ToString() } });

    public static Error TeamSizeMismatch(int expected)
        => Error.Validation(
            code: "TeamEligibility.TeamSizeMismatch",
            description: $"This tournament needs exactly {expected} bowlers.",
            metadata: new Dictionary<string, object> { { "ExpectedTeamSize", expected } });
}
