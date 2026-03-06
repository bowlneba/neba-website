using Neba.Application.Caching;
using Neba.Application.Messaging;

namespace Neba.Application.BowlingCenters.ListBowlingCenters;

/// <summary>
/// A query to retrieve a list of bowling centers, with caching enabled to optimize performance and reduce redundant data retrieval. The cache descriptor and expiry time are defined to ensure efficient caching and timely invalidation of cached data when necessary.
/// </summary>
public sealed record ListBowlingCentersQuery
    : ICachedQuery<IReadOnlyCollection<BowlingCenterSummaryDto>>
{
    /// <summary>
    /// Gets the cache descriptor for this query, which specifies the cache key and associated tags for caching the list of bowling centers. This allows for efficient caching and invalidation of bowling center data when updates occur, ensuring that clients receive up-to-date information while minimizing unnecessary data retrieval. The cache descriptor is defined in the CacheDescriptors class to maintain consistency and prevent key/tag mismatches across the application.
    /// </summary>
    public CacheDescriptor Cache
        => CacheDescriptors.BowlingCenters.List;

    /// <summary>
    /// Gets the expiry time for the cached data, which is set to 7 days. This means that the cached list of bowling centers will be considered valid for up to 7 days before it is automatically invalidated and requires fresh data retrieval. Setting an appropriate expiry time helps balance the benefits of caching with the need for up-to-date information, ensuring that clients receive accurate data while still benefiting from improved performance through caching.
    /// </summary>
    public TimeSpan Expiry
        => TimeSpan.FromDays(7);
}