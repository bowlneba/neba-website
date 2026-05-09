using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using Neba.Application.Caching;
using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Seasons;

namespace Neba.Application.Stats.BoyProgression;

internal sealed class BowlerOfTheYearProgressionService(
    IStatsQueries statsQueries,
    HybridCache cache,
    ILogger<BowlerOfTheYearProgressionService> logger)
    : IBowlerOfTheYearProgressionService
{
    private readonly IStatsQueries _statsQueries = statsQueries;
    private readonly HybridCache _cache = cache;
    private readonly ILogger<BowlerOfTheYearProgressionService> _logger = logger;

    public async Task<IReadOnlyDictionary<int, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>>>
        GetAllProgressionsAsync(SeasonId seasonId, CancellationToken cancellationToken)
    {
        var cacheDescriptor = CacheDescriptors.Stats.BoyProgressions(seasonId);

        return await _cache.GetOrCreateAsync(
            key: cacheDescriptor.Key,
            factory: async (cancel) =>
            {
                _logger.LogCacheMiss(cacheDescriptor.Key);
                var results = await _statsQueries.GetBoyProgressionResultsForSeasonAsync(seasonId, cancel);
                return ComputeAllProgressions(results);
            },
            options: new HybridCacheEntryOptions { Expiration = TimeSpan.FromDays(14) },
            tags: cacheDescriptor.Tags,
            cancellationToken: cancellationToken);
    }

    internal static IReadOnlyDictionary<int, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>>
        ComputeAllProgressions(IReadOnlyCollection<BoyProgressionResultDto> results)
    {
        return new Dictionary<int, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>>
        {
            [BowlerOfTheYearCategory.Open.Value] = ComputeOpenProgression(results),
            [BowlerOfTheYearCategory.Senior.Value] = [],       // Phase 2: requires Bowler.DateOfBirth
            [BowlerOfTheYearCategory.SuperSenior.Value] = [],  // Phase 2: requires Bowler.DateOfBirth
            [BowlerOfTheYearCategory.Woman.Value] = [],        // Phase 2: requires Bowler.Gender
            [BowlerOfTheYearCategory.Youth.Value] = [],        // Phase 2: requires Bowler.DateOfBirth
            [BowlerOfTheYearCategory.Rookie.Value] = [],       // Deferred: requires membership data
        };
    }

    private static IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto> ComputeOpenProgression(
        IReadOnlyCollection<BoyProgressionResultDto> results)
    {
        var eligible = results.Where(r => r.StatsEligible).ToList();
        if (eligible.Count == 0) return [];

        // All stat-eligible tournaments in chronological order — shared X-axis for every series.
        var allTournaments = eligible
            .GroupBy(r => r.TournamentId)
            .Select(g => (Id: g.Key, Name: g.First().TournamentName, Date: g.First().TournamentDate))
            .OrderBy(t => t.Date)
            .ToArray();

        var byBowler = eligible.GroupBy(r => r.BowlerId);

        return [.. byBowler.Select(group =>
        {
            // Collapse main-cut + side-cut rows for the same tournament into one points value.
            var pointsByTournament = group
                .GroupBy(r => r.TournamentId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.SideCutId.HasValue ? 5 : r.Points));

            var cumulativePoints = 0;
            var tournamentResults = allTournaments.Select(t =>
            {
                // Tournaments the bowler didn't enter contribute 0 points — line stays flat.
                if (pointsByTournament.TryGetValue(t.Id, out var pts))
                    cumulativePoints += pts;

                return new BowlerOfTheYearPointsRaceTournamentDto
                {
                    TournamentName = t.Name,
                    TournamentDate = t.Date,
                    CumulativePoints = cumulativePoints
                };
            }).ToArray();

            return new BowlerOfTheYearPointsRaceSeriesDto
            {
                BowlerId = group.Key,
                BowlerName = group.First().BowlerName,
                Results = tournamentResults
            };
        })];
    }
}

internal static partial class BowlerOfTheYearProgressionServiceLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Cache miss for key '{CacheKey}', executing query handler")]
    public static partial void LogCacheMiss(
        this ILogger<BowlerOfTheYearProgressionService> logger,
        string cacheKey);
}
