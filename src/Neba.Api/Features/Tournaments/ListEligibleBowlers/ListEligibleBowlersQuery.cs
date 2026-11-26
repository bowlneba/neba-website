using ErrorOr;

using Neba.Api.Caching;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

internal sealed record ListEligibleBowlersQuery
    : ICachedQuery<ErrorOr<EligibleBowlersDto>>
{
    public required TournamentId Id { get; init; }

    public CacheDescriptor Cache
        => CacheDescriptors.Tournaments.EligibleBowlers(Id);

    // A backstop: the results sync, delete and edit handlers and the bowler jobs clear the list by tag.
    public TimeSpan Expiry
        => TimeSpan.FromDays(1);
}
