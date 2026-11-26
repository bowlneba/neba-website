using FastEndpoints;

namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

internal sealed class ListEligibleBowlersRequest
{
    [BindFrom("id")]
    public required string TournamentId { get; set; }
}