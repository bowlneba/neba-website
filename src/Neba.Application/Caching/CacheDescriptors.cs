
namespace Neba.Application.Caching;

#pragma warning disable CA1724 // Nested type name intentionally mirrors domain for API clarity

/// <summary>
/// Centralized factory for cache descriptors. Each method returns a matched
/// key and tag set, preventing key/tag mismatches at the call site.
/// </summary>
/// <remarks>
/// Key format:  neba:{category}:{identifier}[:{qualifier}]
/// Tag format:  neba:{category} (all), neba:{category}:{identifier} (specific)
/// </remarks>
public static class CacheDescriptors
{
    /// <summary>
    /// Cache descriptors for document content.
    /// </summary>
    public static class Documents
    {
        /// <summary>
        /// Returns a cache descriptor for a document's content, identified by the given document key.
        /// </summary>
        /// <param name="documentKey">The key of the document.</param>
        /// <returns>A cache descriptor for the document's content.</returns>
        public static CacheDescriptor Content(string documentKey)
            => new()
            {
                Key = $"neba:document:{documentKey}:content",
                Tags = ["neba:documents", $"neba:document:{documentKey}"]
            };
    }

    /// <summary>
    /// Cache descriptors for bowling center data.
    /// </summary>
    public static class BowlingCenters
    {
        /// <summary>
        /// Returns a cache descriptor for the list of bowling centers, with a key and tags that allow for efficient caching and invalidation of bowling center data.
        /// </summary>
        public static CacheDescriptor List
            => new()
            {
                Key = "neba:bowling-centers:list",
                Tags = ["neba:bowling-centers"]
            };
    }
}
#pragma warning restore CA1724