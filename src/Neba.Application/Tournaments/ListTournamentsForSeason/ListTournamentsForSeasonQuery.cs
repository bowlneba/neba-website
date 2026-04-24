using Neba.Application.Caching;
using Neba.Application.Messaging;
using Neba.Domain.Seasons;

namespace Neba.Application.Tournaments.ListTournamentsForSeason;

/// <summary>
/// A query to list all tournaments for a given season.
/// </summary>
public sealed record ListTournamentsForSeasonQuery
    : ICachedQuery<IReadOnlyCollection<TournamentSummaryDto>>
{
    /// <summary>
    /// The ID of the season to list tournaments for.
    /// </summary>
    public required SeasonId SeasonId { get; init; }

    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.Tournaments.ListForSeason(SeasonId);

    /// <inheritdoc />
    public TimeSpan Expiry
        => TimeSpan.FromDays(14);
}