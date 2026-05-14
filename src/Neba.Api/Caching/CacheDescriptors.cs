
namespace Neba.Api.Caching;

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
    /// Cache descriptors for sponsor data.
    /// </summary>
    public static class Sponsors
    {
        /// <summary>
        /// Returns a cache descriptor for the list of active sponsors, with a key and tags that allow for efficient caching and invalidation of sponsor data.
        /// </summary>
        public static CacheDescriptor ListActiveSponsors
            => new()
            {
                Key = "neba:sponsors:active:list",
                Tags = ["neba", "neba:sponsors"]
            };

        /// <summary>
        /// Returns a cache descriptor for sponsor detail data identified by the given slug.
        /// </summary>
        /// <param name="slug">The sponsor slug.</param>
        /// <returns>A cache descriptor for sponsor detail data.</returns>
        public static CacheDescriptor Detail(string slug)
            => new()
            {
                Key = $"neba:sponsors:{slug}:detail",
                Tags = ["neba", "neba:sponsors", $"neba:sponsors:{slug}"]
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

    /// <summary>
    /// Cache descriptors for season data.
    /// </summary>
    public static class Seasons
    {
        /// <summary>
        /// Returns a cache descriptor for the list of seasons, with a key and tags that allow for efficient caching and invalidation of season data.
        /// </summary>
        public static CacheDescriptor List
            => new()
            {
                Key = "neba:seasons:list",
                Tags = ["neba", "neba:seasons"]
            };
    }

    /// <summary>
    /// Cache descriptors for stats data.
    /// </summary>
    public static class Stats
    {
        /// <summary>
        /// Returns a cache descriptor for the list of seasons with stats.
        /// </summary>
        public static CacheDescriptor ListSeasonsWithStats
            => new()
            {
                Key = "neba:stats:seasons:list",
                Tags = ["neba", "neba:stats", "neba:stats:seasons"]
            };

        /// <summary>
        /// Returns a cache descriptor for bowler season stats for the given season.
        /// </summary>
        /// <param name="seasonId">The season identifier.</param>
        /// <returns>A cache descriptor for bowler season stats.</returns>
        public static CacheDescriptor BowlerSeasonStats(SeasonId seasonId)
            => new()
            {
                Key = $"neba:stats:seasons:{seasonId}:bowlers",
                Tags = ["neba", "neba:stats", "neba:stats:seasons", $"neba:stats:seasons:{seasonId}"]
            };

        /// <summary>
        /// Returns a cache descriptor for all BOY race progressions for the given season.
        /// </summary>
        /// <param name="seasonId">The season identifier.</param>
        /// <returns>A cache descriptor for BOY progression data.</returns>
        public static CacheDescriptor BoyProgressions(SeasonId seasonId)
            => new()
            {
                Key = $"neba:stats:seasons:{seasonId}:boy-progressions",
                Tags = ["neba", "neba:stats", "neba:stats:seasons", $"neba:stats:seasons:{seasonId}"]
            };
    }

    /// <summary>
    /// Cache descriptors for tournament data.
    /// </summary>
    public static class Tournaments
    {
        /// <summary>
        /// Returns a cache descriptor for the list of tournaments in a given season.
        /// </summary>
        /// <param name="seasonId">The season identifier.</param>
        /// <returns>A cache descriptor for the tournaments in the season.</returns>
        public static CacheDescriptor ListForSeason(SeasonId seasonId)
            => new()
            {
                Key = $"neba:tournaments:{seasonId}:list",
                Tags = ["neba", "neba:tournaments", $"neba:tournaments:{seasonId}"]
            };

        /// <summary>
        /// Returns a cache descriptor for the details of a specific tournament, identified by the given tournament ID.
        /// </summary>
        /// <param name="id">The tournament identifier.</param>
        /// <returns>A cache descriptor for the tournament details.</returns>
        public static CacheDescriptor TournamentDetail(TournamentId id)
            => new()
            {
                Key = $"neba:tournaments:{id}",
                Tags = ["neba", "neba:tournaments", $"neba:tournaments:{id}"]
            };
    }
}
#pragma warning restore CA1724