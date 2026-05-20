using ErrorOr;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments;

internal static class TournamentErrors
{
    public static Error TournamentNotFound(TournamentId id)
        => Error.NotFound(
            code: "Tournament.NotFound",
            description: "Tournament was not found.",
            metadata: new Dictionary<string, object>
            {
                { "TournamentId", id.ToString() }
            });
}