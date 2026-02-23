namespace Neba.Application.Caching;

/// <summary>
/// Represents a descriptor for caching information, including the cache key and associated tags.
/// </summary>
public readonly record struct CacheDescriptor
{
    /// <summary>
    /// Gets the cache key associated with this descriptor, used to identify cached entries.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the tags associated with this cache descriptor, used for cache invalidation and organization.
    /// </summary>
    public required IReadOnlyCollection<string> Tags { get; init; }
}