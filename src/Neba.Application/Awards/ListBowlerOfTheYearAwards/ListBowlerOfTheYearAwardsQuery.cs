using Neba.Application.Caching;
using Neba.Application.Messaging;

namespace Neba.Application.Awards.ListBowlerOfTheYearAwards;

/// <summary>
/// Query to retrieve a list of Bowler of the Year awards, which recognize overall performance
/// across Stat-Eligible Tournaments during a Season. This query is designed to be cached for
/// efficient retrieval, with a long expiry time since the data is not expected to change frequently.
/// </summary>
public sealed record ListBowlerOfTheYearAwardsQuery
    : ICachedQuery<IReadOnlyCollection<BowlerOfTheYearAwardDto>>
{
    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.Awards.ListBowlerOfTheYearAwards;

    /// <inheritdoc />
    public TimeSpan Expiry
        => new(365, 0, 0, 0); // 1 year
}
