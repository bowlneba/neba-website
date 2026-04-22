using Neba.Application.Caching;
using Neba.Application.Messaging;

namespace Neba.Application.Seasons.ListSeasons;

/// <summary>
/// Query for retrieving a list of seasons, ordered by start date descending. Results are cached for 90 days, as season data is relatively static and infrequently updated.
/// </summary>
public sealed record ListSeasonsQuery
    : ICachedQuery<IReadOnlyCollection<SeasonDto>>
{
    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.Seasons.List;

    /// <inheritdoc />
    public TimeSpan Expiry
        => TimeSpan.FromDays(90);
}