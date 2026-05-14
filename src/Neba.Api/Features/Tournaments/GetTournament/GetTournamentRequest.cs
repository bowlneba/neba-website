using FastEndpoints;

namespace Neba.Api.Features.Tournaments.GetTournament;

internal sealed class GetTournamentRequest
{
    [BindFrom("id")]
    public required string TournamentId { get; set; }
}