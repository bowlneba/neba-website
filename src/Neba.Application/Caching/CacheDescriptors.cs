
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
                Tags = ["neba", "neba:documents", $"neba:document:{documentKey}"]
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
                Tags = ["neba", "neba:bowling-centers"]
            };
    }

    /// <summary>
    /// Cache descriptors for Hall of Fame data.
    /// </summary>
    public static class HallOfFame
    {
        /// <summary>
        /// Returns a cache descriptor for the list of Hall of Fame inductions, with a key and tags that allow for efficient caching and invalidation of Hall of Fame data.
        /// </summary>
        public static CacheDescriptor ListInductions
            => new()
            {
                Key = "neba:hall-of-fame:inductions:list",
                Tags = ["neba", "neba:hall-of-fame", "neba:hall-of-fame:inductions"]
            };
    }

    /// <summary>
    /// Cache descriptors for awards data.
    /// </summary>
    public static class Awards
    {
        /// <summary>
        /// Returns a cache descriptor for the list of high block awards, with a key and tags that allow for efficient caching and invalidation of awards data.
        /// </summary>
        public static CacheDescriptor ListHighBlockAwards
            => new()
            {
                Key = "neba:awards:high-block:list",
                Tags = ["neba", "neba:awards", "neba:awards:high-block"]
            };

        /// <summary>
        /// Returns a cache descriptor for the list of high average awards, with a key and tags that allow for efficient caching and invalidation of awards data.
        /// </summary>
        public static CacheDescriptor ListHighAverageAwards
            => new()
            {
                Key = "neba:awards:high-average:list",
                Tags = ["neba", "neba:awards", "neba:awards:high-average"]
            };

        /// <summary>
        /// Returns a cache descriptor for the list of Bowler of the Year awards, with a key and tags that allow for efficient caching and invalidation of awards data.
        /// </summary>
        public static CacheDescriptor ListBowlerOfTheYearAwards
            => new()
            {
                Key = "neba:awards:bowler-of-the-year:list",
                Tags = ["neba", "neba:awards", "neba:awards:bowler-of-the-year"]
            };
    }
}
#pragma warning restore CA1724