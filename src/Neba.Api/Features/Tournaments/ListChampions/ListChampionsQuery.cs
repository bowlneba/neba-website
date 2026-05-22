using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListChampions;

/// <summary>
/// Represents a query to list tournament champions. This query is used to retrieve a list of champions for all tournaments, including the bowler's name, the tournament they won, and other relevant details. The results of this query are cached for 14 days to improve performance and reduce load on the database, as the list of champions does not change frequently.
/// </summary>
public sealed record ListChampionsQuery
    : ICachedQuery<IReadOnlyCollection<TournamentChampionsDto>>
{
    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.Tournaments.ListChampions;

    /// <inheritdoc />
    public TimeSpan Expiry
        => TimeSpan.FromDays(14);
}