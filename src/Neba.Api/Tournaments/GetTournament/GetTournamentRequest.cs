using FastEndpoints;

namespace Neba.Api.Tournaments.GetTournament;

internal sealed class GetTournamentRequest
{
    [BindFrom("id")]
    public required string TournamentId { get; set; }
}
