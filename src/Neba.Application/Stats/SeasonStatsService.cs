using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using Neba.Application.Caching;
using Neba.Application.Seasons;
using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Seasons;

namespace Neba.Application.Stats;

internal sealed class SeasonStatsService(
    IStatsQueries statsQueries,
    HybridCache cache,
    ILogger<SeasonStatsService> logger)
        : ISeasonStatsService
{
    private readonly IStatsQueries _statsQueries = statsQueries;
    private readonly HybridCache _cache = cache;
    private readonly ILogger<SeasonStatsService> _logger = logger;

    public async Task<IReadOnlyCollection<SeasonDto>> GetSeasonsWithStatsAsync(CancellationToken cancellationToken)
    {
        var seasons = await _cache.GetOrCreateAsync(
            key: CacheDescriptors.Stats.ListSeasonsWithStats.Key,
            factory: async (cancel) =>
            {
                _logger.LogCacheMiss(CacheDescriptors.Stats.ListSeasonsWithStats.Key);

                return await _statsQueries.GetSeasonsWithStatsAsync(cancel);
            },
            options: new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromDays(7)
            },
            tags: CacheDescriptors.Stats.ListSeasonsWithStats.Tags,
            cancellationToken: cancellationToken);

        return seasons;
    }

    public async Task<IReadOnlyCollection<BowlerSeasonStatsDto>> GetBowlerSeasonStatsAsync(SeasonId seasonId, CancellationToken cancellationToken)
    {
        var stats = await _cache.GetOrCreateAsync(
            key: CacheDescriptors.Stats.BowlerSeasonStats(seasonId).Key,
            factory: async (cancel) =>
            {
                _logger.LogCacheMiss(CacheDescriptors.Stats.BowlerSeasonStats(seasonId).Key);

                return await _statsQueries.GetBowlerSeasonStatsAsync(seasonId, cancel);
            },
            options: new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromDays(14)
            },
            tags: CacheDescriptors.Stats.BowlerSeasonStats(seasonId).Tags,
            cancellationToken: cancellationToken
        );

        return stats;
    }

    public SeasonStatsSummaryDto CalculateSeasonStatsSummary(IReadOnlyCollection<BowlerSeasonStatsDto> bowlerStats)
    {
        var qualifyingHighGame = bowlerStats.Max(stat => stat.QualifyingHighGame);
        var matchPlayHighGame = bowlerStats.Max(stat => stat.MatchPlayHighGame);
        var highGame = Math.Max(qualifyingHighGame, matchPlayHighGame);
        var bowlersWithHighGame = bowlerStats
            .Where(stat => stat.QualifyingHighGame == highGame || stat.MatchPlayHighGame == highGame)
            .ToDictionary(stat => stat.BowlerId, stat => stat.BowlerName);

        var highBlock = bowlerStats.Max(stat => stat.HighBlock);
        var bowlersWithHighBlock = bowlerStats
            .Where(stat => stat.HighBlock == highBlock)
            .ToDictionary(stat => stat.BowlerId, stat => stat.BowlerName);

        var highAverage = bowlerStats.Max(stat => stat.TotalPinfall / stat.TotalGames * 1m);
        var bowlersWithHighAverage = bowlerStats
            .Where(stat => stat.TotalPinfall / stat.TotalGames * 1m == highAverage)
            .ToDictionary(stat => stat.BowlerId, stat => stat.BowlerName);

        var highestMatchPlayWinPercentage = bowlerStats.Max(stat =>
            stat.MatchPlayWins + stat.MatchPlayLosses > 0
                ? stat.MatchPlayWins * 1m / (stat.MatchPlayWins + stat.MatchPlayLosses)
                : 0);
        var bowlersWithHighestMatchPlayWinPercentage = bowlerStats
            .Where(stat => stat.MatchPlayWins + stat.MatchPlayLosses > 0 && stat.MatchPlayWins * 1m / (stat.MatchPlayWins + stat.MatchPlayLosses) == highestMatchPlayWinPercentage)
            .ToDictionary(stat => stat.BowlerId, stat => stat.BowlerName);

        var mostFinals = bowlerStats.Max(stat => stat.Finals);
        var bowlersWithMostFinals = bowlerStats
            .Where(stat => stat.Finals == mostFinals)
            .ToDictionary(stat => stat.BowlerId, stat => stat.BowlerName);

        return new SeasonStatsSummaryDto
        {
            TotalEntries = bowlerStats.Sum(stat => stat.TotalEntries),
            TotalPrizeMoney = bowlerStats.Sum(stat => stat.TournamentWinnings),
            HighGame = highGame,
            HighGameBowlers = bowlersWithHighGame,
            HighBlock = highBlock,
            HighBlockBowlers = bowlersWithHighBlock,
            HighAverage = highAverage,
            HighAverageBowlers = bowlersWithHighAverage,
            HighestMatchPlayWinPercentage = highestMatchPlayWinPercentage,
            HighestMatchPlayWinPercentageBowlers = bowlersWithHighestMatchPlayWinPercentage,
            MostFinals = mostFinals,
            MostFinalsBowlers = bowlersWithMostFinals
        };
    }
}

internal interface ISeasonStatsService
{
    Task<IReadOnlyCollection<SeasonDto>> GetSeasonsWithStatsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<BowlerSeasonStatsDto>> GetBowlerSeasonStatsAsync(SeasonId seasonId, CancellationToken cancellationToken);

    SeasonStatsSummaryDto CalculateSeasonStatsSummary(IReadOnlyCollection<BowlerSeasonStatsDto> bowlerStats);
}

internal static partial class SeasonStatsServiceLogMessages
{
    [LoggerMessage(
    Level = LogLevel.Information,
    Message = "Cache miss for key '{CacheKey}', executing query handler")]
    public static partial void LogCacheMiss(
    this ILogger<SeasonStatsService> logger,
    string cacheKey);
}