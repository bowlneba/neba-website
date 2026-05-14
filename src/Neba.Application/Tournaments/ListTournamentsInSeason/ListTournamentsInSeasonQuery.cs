using Neba.Application.Caching;
using Neba.Application.Messaging;
using Neba.Domain.Seasons;

namespace Neba.Application.Tournaments.ListTournamentsInSeason;

/// <summary>
/// A query to list all tournaments for a given season.
/// </summary>
public sealed record ListTournamentsInSeasonQuery
    : ICachedQuery<IReadOnlyCollection<SeasonTournamentDto>>
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