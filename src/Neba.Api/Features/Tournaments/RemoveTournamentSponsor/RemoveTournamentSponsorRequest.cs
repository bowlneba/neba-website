using FastEndpoints;

namespace Neba.Api.Features.Tournaments.RemoveTournamentSponsor;

internal sealed class RemoveTournamentSponsorRequest
{
    [BindFrom("id")]
    public required string TournamentId { get; set; }

    public required string SponsorId { get; set; }
}