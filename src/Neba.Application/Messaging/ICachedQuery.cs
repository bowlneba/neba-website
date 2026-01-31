namespace Neba.Application.Messaging;

/// <summary>
/// Represents a query that supports caching and returns a response of type <typeparamref name="TResponse"/>.
/// Used in the CQRS pattern to encapsulate a request for data with caching capabilities.
/// </summary>
/// <typeparam name="TResponse">The type of the response returned by the query.</typeparam>
public interface ICachedQuery<out TResponse>
    : IQuery<TResponse>
{
    /// <summary>
    /// Gets the cache key associated with this query.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Gets the expiry duration for the cached response.
    /// </summary>
    TimeSpan Expiry { get; }

    /// <summary>
    /// Gets the tags associated with this cached query for cache invalidation purposes.
    /// </summary>
    IReadOnlyCollection<string> Tags { get; }
}