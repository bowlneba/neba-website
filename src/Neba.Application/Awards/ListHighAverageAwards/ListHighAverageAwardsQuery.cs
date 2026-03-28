using Neba.Application.Caching;
using Neba.Application.Messaging;

namespace Neba.Application.Awards.ListHighAverageAwards;

/// <summary>
/// Query to retrieve a list of high average awards, which are given to players who have achieved a high average score in a bowling season. This query is designed to be cached for efficient retrieval, with a long expiry time since the data is not expected to change frequently.
/// </summary>
public sealed record ListHighAverageAwardsQuery
    : ICachedQuery<IReadOnlyCollection<HighAverageAwardDto>>
{
    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.Awards.ListHighAverageAwards;

    /// <inheritdoc />
    public TimeSpan Expiry
        => new(365, 0, 0, 0); // 1 year
}