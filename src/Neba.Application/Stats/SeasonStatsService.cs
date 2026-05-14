using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using Neba.Domain.Seasons;

namespace Neba.Application.Stats;

internal sealed class SeasonStatsService(
    IStatsQueries statsQueries,
    ITournamentQueries tournamentQueries,
    HybridCache cache,
    ILogger<SeasonStatsService> logger)
        : ISeasonStatsService
{
    private readonly IStatsQueries _statsQueries = statsQueries;
    private readonly ITournamentQueries _tournamentQueries = tournamentQueries;

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
        var cacheDescriptor = CacheDescriptors.Stats.BowlerSeasonStats(seasonId);

        var stats = await _cache.GetOrCreateAsync(
            key: cacheDescriptor.Key,
            factory: async (cancel) =>
            {
                _logger.LogCacheMiss(cacheDescriptor.Key);

                return await _statsQueries.GetBowlerSeasonStatsAsync(seasonId, cancel);
            },
            options: new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromDays(14)
            },
            tags: cacheDescriptor.Tags,
            cancellationToken: cancellationToken
        );

        return stats;
    }
}

internal interface ISeasonStatsService
{
    Task<IReadOnlyCollection<BowlerSeasonStatsDto>> GetBowlerSeasonStatsAsync(SeasonId seasonId, CancellationToken cancellationToken);
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