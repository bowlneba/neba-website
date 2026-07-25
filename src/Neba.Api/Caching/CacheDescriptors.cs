
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

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
    private const string ManagementScope = "management";
    private const string PublicScope = "public";
    private const string AuthenticatedScope = "authenticated";

    /// <summary>
    /// Cache descriptors for bowler data.
    /// </summary>
    public static class Bowlers
    {
        /// <summary>
        /// Returns a cache descriptor for the titles won by a specific bowler.
        /// </summary>
        /// <param name="bowlerId">
        /// The bowler identifier.
        /// </param>
        /// <returns>
        /// A cache descriptor for the bowler's titles.
        /// </returns>
        public static CacheDescriptor Titles(BowlerId bowlerId)
            => new()
            {
                Key = $"neba:bowlers:{bowlerId}:titles",
                Tags = ["neba", "neba:bowlers", $"neba:bowlers:{bowlerId}"]
            };
    }

    /// <summary>
    /// Cache descriptors for document content.
    /// </summary>
    public static class Documents
    {
        /// <summary>
        /// Returns a cache descriptor for a document's content, identified by the given document key.
        /// </summary>
        /// <param name="documentKey">
        /// The key of the document.
        /// </param>
        /// <returns>
        /// A cache descriptor for the document's content.
        /// </returns>
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
    /// Cache descriptors for static/rarely-changing reference (lookup) data.
    /// </summary>
    public static class ReferenceData
    {
        /// <summary>
        /// Returns a cache descriptor for the list of US states, with a key and tags that allow for efficient caching and invalidation of reference data.
        /// </summary>
        public static CacheDescriptor UsStates
            => new()
            {
                Key = "neba:reference-data:us-states:list",
                Tags = ["neba", "neba:reference-data"]
            };

        /// <summary>
        /// Returns a cache descriptor for the list of phone number types, with a key and tags that allow for efficient caching and invalidation of reference data.
        /// </summary>
        public static CacheDescriptor PhoneNumberTypes
            => new()
            {
                Key = "neba:reference-data:phone-number-types:list",
                Tags = ["neba", "neba:reference-data"]
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
        public static CacheDescriptor ListActiveSponsors(bool callerHasSponsorManagementPermission)
            => new()
            {
                Key = $"neba:sponsors:list:scope:{(callerHasSponsorManagementPermission ? ManagementScope : PublicScope)}",
                Tags = ["neba", "neba:sponsors"]
            };

        /// <summary>
        /// Returns a cache descriptor for sponsor detail data identified by the given slug.
        /// </summary>
        /// <param name="slug">
        /// The sponsor slug.
        /// </param>
        /// <param name="callerHasSponsorManagementPermission">
        /// Whether the caller can see a sponsor that isn't the current/active one — kept separate from
        /// the public cache entry, so a management-scoped response is never served to an anonymous caller.
        /// </param>
        /// <returns>
        /// A cache descriptor for sponsor detail data.
        /// </returns>
        public static CacheDescriptor Detail(string slug, bool callerHasSponsorManagementPermission)
            => new()
            {
                Key = $"neba:sponsors:{slug}:detail:scope:{(callerHasSponsorManagementPermission ? ManagementScope : PublicScope)}",
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
        /// <param name="seasonId">
        /// The season identifier.
        /// </param>
        /// <returns>
        /// A cache descriptor for bowler season stats.
        /// </returns>
        public static CacheDescriptor BowlerSeasonStats(SeasonId seasonId)
            => new()
            {
                Key = $"neba:stats:seasons:{seasonId}:bowlers",
                Tags = ["neba", "neba:stats", "neba:stats:seasons", $"neba:stats:seasons:{seasonId}"]
            };

        /// <summary>
        /// Returns a cache descriptor for all BOY race progressions for the given season.
        /// </summary>
        /// <param name="seasonId">
        /// The season identifier.
        /// </param>
        /// <returns>
        /// A cache descriptor for BOY progression data.
        /// </returns>
        public static CacheDescriptor BoyProgressions(SeasonId seasonId)
            => new()
            {
                Key = $"neba:stats:seasons:{seasonId}:boy-progressions",
                Tags = ["neba", "neba:stats", "neba:stats:seasons", $"neba:stats:seasons:{seasonId}"]
            };
    }

    /// <summary>
    /// Cache descriptors for news data.
    /// </summary>
    public static class News
    {
        /// <summary>
        /// Returns a cache descriptor for a paginated list of article summaries.
        /// </summary>
        /// <param name="page">
        /// The page number.
        /// </param>
        /// <param name="pageSize">
        /// The number of items per page.
        /// </param>
        /// <param name="callerHasArticleManagementPermission">
        /// Indicates whether the caller has permission to manage articles.
        /// </param>
        /// <returns>
        /// A cache descriptor for the paginated article list.
        /// </returns>
        public static CacheDescriptor ListArticles(int page, int pageSize, bool callerHasArticleManagementPermission)
        => new()
        {
            Key = $"neba:news:articles:list:page:{page}:size:{pageSize}:scope:{(callerHasArticleManagementPermission ? ManagementScope : PublicScope)}",
            Tags = ["neba", "neba:news", "neba:news:articles"]
        };

        /// <summary>
        /// Returns a cache descriptor for a specific news article identified by its slug.
        /// </summary>
        /// <param name="slug">
        /// The article slug.
        /// </param>
        /// <param name="callerHasArticleManagementPermission">
        /// Indicates whether the caller has permission to manage articles.
        /// </param>
        /// <returns>
        /// A cache descriptor for the article detail.
        /// </returns>
        public static CacheDescriptor Article(string slug, bool callerHasArticleManagementPermission)
        => new()
        {
            Key = $"neba:news:{slug}:article:scope:{(callerHasArticleManagementPermission ? ManagementScope : PublicScope)}",
            Tags = ["neba", "neba:news", $"neba:news:{slug}"]
        };
    }

    /// <summary>
    /// Cache descriptors for tournament data.
    /// </summary>
    public static class Tournaments
    {
        private const string Tag = "neba:tournaments";

        private static string ResolveScope(bool callerIsAuthenticated, bool callerHasTournamentManagementPermission)
        {
            if (callerHasTournamentManagementPermission)
            {
                return ManagementScope;
            }

            return callerIsAuthenticated ? AuthenticatedScope : PublicScope;
        }

        /// <summary>
        /// Returns a cache descriptor for the list of tournaments in a given season.
        /// </summary>
        /// <param name="seasonId">
        /// The season identifier.
        /// </param>
        /// <param name="callerIsAuthenticated">
        /// Whether the caller is authenticated — an authenticated caller sees the oil pattern reveal
        /// date/time even before it passes, so this is cached separately from an anonymous response.
        /// </param>
        /// <param name="callerHasTournamentManagementPermission">
        /// Whether the caller holds the tournament management permission — such a caller sees full oil
        /// pattern details even before the reveal date passes, so this is cached separately from both
        /// the anonymous and authenticated-but-non-management responses.
        /// </param>
        /// <returns>
        /// A cache descriptor for the tournaments in the season.
        /// </returns>
        public static CacheDescriptor ListForSeason(SeasonId seasonId, bool callerIsAuthenticated, bool callerHasTournamentManagementPermission)
            => new()
            {
                Key = $"{Tag}:{seasonId}:list:scope:{ResolveScope(callerIsAuthenticated, callerHasTournamentManagementPermission)}",
                Tags = ["neba", Tag, $"{Tag}:{seasonId}"]
            };

        /// <summary>
        /// Returns a cache descriptor for the details of a specific tournament, identified by the given tournament ID.
        /// </summary>
        /// <param name="id">
        /// The tournament identifier.
        /// </param>
        /// <param name="callerIsAuthenticated">
        /// Whether the caller is authenticated — an authenticated caller sees the oil pattern reveal
        /// date/time even before it passes, so this is cached separately from an anonymous response.
        /// </param>
        /// <param name="callerHasTournamentManagementPermission">
        /// Whether the caller holds the tournament management permission — such a caller sees full oil
        /// pattern details even before the reveal date passes, so this is cached separately from both
        /// the anonymous and authenticated-but-non-management responses.
        /// </param>
        /// <returns>
        /// A cache descriptor for the tournament details.
        /// </returns>
        public static CacheDescriptor TournamentDetail(TournamentId id, bool callerIsAuthenticated, bool callerHasTournamentManagementPermission)
            => new()
            {
                Key = $"{Tag}:{id}:scope:{ResolveScope(callerIsAuthenticated, callerHasTournamentManagementPermission)}",
                Tags = ["neba", Tag, $"{Tag}:{id}"]
            };

        /// <summary>
        /// Returns a cache descriptor for the list of all tournament champions.
        /// </summary>
        public static CacheDescriptor ListChampions
            => new()
            {
                Key = $"{Tag}:champions:list",
                Tags = ["neba", Tag, $"{Tag}:champions"]
            };

        /// <summary>
        /// Returns a cache descriptor for the list of active tournament types.
        /// </summary>
        public static CacheDescriptor Types
            => new()
            {
                Key = $"{Tag}:types:list",
                Tags = ["neba", Tag, $"{Tag}:types"]
            };
    }

    /// <summary>
    /// Cache descriptors for oil patterns.
    /// </summary>
    /// <remarks>
    /// Key format:  neba:{category}:{identifier}[:{qualifier}]
    /// Tag format:  neba:{category} (all), neba:{category}:{identifier} (specific)
    /// </remarks>
    public static class OilPatterns
    {
        /// <summary>
        /// Returns a cache descriptor for the list of oil patterns, with a key and tags that allow
        /// for efficient caching and invalidation of oil pattern data.
        /// </summary>
        public static CacheDescriptor List
            => new()
            {
                Key = "neba:oil-patterns:list",
                Tags = ["neba", "neba:oil-patterns"]
            };
    }
}
#pragma warning restore CA1724