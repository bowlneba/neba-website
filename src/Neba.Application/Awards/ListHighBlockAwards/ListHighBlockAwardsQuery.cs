using Neba.Application.Caching;
using Neba.Application.Messaging;

namespace Neba.Application.Awards.ListHighBlockAwards;

/// <summary>
/// Query to retrieve a list of high block awards, which are given to players who have achieved a high block score in a bowling game. This query is designed to be cached for efficient retrieval, with a long expiry time since the data is not expected to change frequently.
/// </summary>
public sealed record ListHighBlockAwardsQuery
    : ICachedQuery<IReadOnlyCollection<HighBlockAwardDto>>
{
    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.Awards.ListHighBlockAwards;

    /// <inheritdoc />
    public TimeSpan Expiry
        => new(365, 0, 0, 0); // 1 year
}