using Neba.Application.Caching;

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
    /// Gets the cache descriptor containing the cache key and associated tags for this query, used to identify and manage cached entries.
    /// </summary>
    CacheDescriptor Cache { get; }

    /// <summary>
    /// Gets the expiry duration for the cached response.
    /// </summary>
    TimeSpan Expiry { get; }
}