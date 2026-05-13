using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using Neba.Application.Caching;
using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;
using Neba.Domain.Tournaments;

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
            [BowlerOfTheYearCategory.Open.Value] = ComputeRaceProgression(results, BowlerOfTheYearCategory.Open),
            [BowlerOfTheYearCategory.Senior.Value] = ComputeRaceProgression(results, BowlerOfTheYearCategory.Senior),
            [BowlerOfTheYearCategory.SuperSenior.Value] = ComputeRaceProgression(results, BowlerOfTheYearCategory.SuperSenior),
            [BowlerOfTheYearCategory.Woman.Value] = ComputeRaceProgression(results, BowlerOfTheYearCategory.Woman),
            [BowlerOfTheYearCategory.Youth.Value] = ComputeRaceProgression(results, BowlerOfTheYearCategory.Youth),
            [BowlerOfTheYearCategory.Rookie.Value] = [],  // Deferred: requires membership data
        };
    }

    private static IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto> ComputeRaceProgression(
        IReadOnlyCollection<BoyProgressionResultDto> results,
        BowlerOfTheYearCategory category)
    {
        var eligible = results
            .Where(r => IsTournamentEligibleForRace(r, category) && IsBowlerEligibleForRace(r, category))
            .ToList();

        if (eligible.Count == 0) return [];

        // All tournaments for this race in chronological order — shared X-axis for every series.
        // Deduplicate names: when two tournaments share a display name, append the date so each
        // chart category label is unique (duplicate names cause ApexCharts to drop data points).
        var allTournaments = eligible
            .GroupBy(r => r.TournamentId)
            .Select(g => (Id: g.Key, Name: g.First().TournamentName, Date: g.First().TournamentDate))
            .OrderBy(t => t.Date)
            .ToArray();

        var duplicateNames = allTournaments
            .GroupBy(t => t.Name)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .Select(t => t.Id)
            .ToHashSet();

        allTournaments = [.. allTournaments.Select(t =>
            duplicateNames.Contains(t.Id)
                ? (t.Id, $"{t.Name} ({t.Date:M/d})", t.Date)
                : t)];

        var byBowler = eligible.GroupBy(r => r.BowlerId);

        return [.. byBowler.Select(group =>
        {
            // Collapse multiple rows for the same tournament into one points value.
            var pointsByTournament = group
                .GroupBy(r => r.TournamentId)
                .ToDictionary(g => g.Key, g => g.Sum(r => PointsForRace(r, category)));

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

    private static bool IsTournamentEligibleForRace(BoyProgressionResultDto r, BowlerOfTheYearCategory category)
    {
        if (category == BowlerOfTheYearCategory.Open
            || category == BowlerOfTheYearCategory.Youth
            || category == BowlerOfTheYearCategory.Rookie)
        {
            return r.StatsEligible;
        }

        if (category == BowlerOfTheYearCategory.Senior
            || category == BowlerOfTheYearCategory.SuperSenior)
        {
            return r.StatsEligible
                || r.TournamentType == TournamentType.Senior
                || r.TournamentType == TournamentType.SeniorAndWomen;
        }

        return category == BowlerOfTheYearCategory.Woman
        && (r.StatsEligible
            || r.TournamentType == TournamentType.Women
            || r.TournamentType == TournamentType.SeniorAndWomen
        );
    }

    private static bool IsBowlerEligibleForRace(BoyProgressionResultDto r, BowlerOfTheYearCategory category)
    {
        if (category == BowlerOfTheYearCategory.Open)
            return true;

        if (category == BowlerOfTheYearCategory.Woman)
            return r.BowlerGender == Gender.Female;

        if (category == BowlerOfTheYearCategory.Senior)
            return r.BowlerDateOfBirth.HasValue && AgeAt(r.BowlerDateOfBirth.Value, r.TournamentEndDate) >= 50;

        return category == BowlerOfTheYearCategory.SuperSenior
            ? r.BowlerDateOfBirth.HasValue && AgeAt(r.BowlerDateOfBirth.Value, r.TournamentEndDate) >= 60
            : category == BowlerOfTheYearCategory.Youth
&& r.BowlerDateOfBirth.HasValue && AgeAt(r.BowlerDateOfBirth.Value, r.TournamentEndDate) < 18;
    }

    // Age completed on evaluationDate. Uses DateOnly.AddYears so Feb-29 birthdays are handled correctly.
    private static int AgeAt(DateOnly dateOfBirth, DateOnly evaluationDate)
    {
        var age = evaluationDate.Year - dateOfBirth.Year;
        if (dateOfBirth.AddYears(age) > evaluationDate) age--;
        return age;
    }

    private static int PointsForRace(BoyProgressionResultDto r, BowlerOfTheYearCategory category)
    {
        if (r.SideCutId is null)
            return r.Points;

        return DeriveSideCutBoyCategory(r.SideCutName) == category ? r.Points : 5;
    }

    private static BowlerOfTheYearCategory? DeriveSideCutBoyCategory(string? sideCutName) => sideCutName switch
    {
        "Senior" => BowlerOfTheYearCategory.Senior,
        "Super Senior" => BowlerOfTheYearCategory.SuperSenior,
        "Woman" or "Women" => BowlerOfTheYearCategory.Woman,
        _ => null
    };
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