using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using Neba.Application.Bowlers;
using Neba.Application.Caching;
using Neba.Application.Seasons;
using Neba.Application.Stats.GetSeasonStats;
using Neba.Application.Tournaments;
using Neba.Domain.Seasons;

namespace Neba.Application.Stats;

internal sealed class SeasonStatsService(
    IStatsQueries statsQueries,
    IBowlerQueries bowlerQueries,
    ITournamentQueries tournamentQueries,
    HybridCache cache,
    ILogger<SeasonStatsService> logger)
        : ISeasonStatsService
{
    private readonly IStatsQueries _statsQueries = statsQueries;
    private readonly IBowlerQueries _bowlerQueries = bowlerQueries;
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

    public SeasonStatsSummaryDto CalculateSeasonStatsSummary(
        IReadOnlyCollection<BowlerSeasonStatsDto> bowlerStats,
        decimal minimumGames,
        decimal minimumTournaments,
        decimal minimumEntries)
    {
        // Season Bests

        var qualifyingHighGame = bowlerStats.Max(stat => stat.QualifyingHighGame);
        var matchPlayHighGame = bowlerStats.Max(stat => stat.MatchPlayHighGame);
        var highGame = Math.Max(qualifyingHighGame, matchPlayHighGame);
        var highGameBowlers = bowlerStats
            .Where(stat => stat.QualifyingHighGame == highGame || stat.MatchPlayHighGame == highGame)
            .ToDictionary(stat => stat.BowlerId, stat => stat.BowlerName);

        var highBlock = bowlerStats.Max(stat => stat.HighBlock);
        var highBlockBowlers = bowlerStats
            .Where(stat => stat.HighBlock == highBlock)
            .ToDictionary(stat => stat.BowlerId, stat => stat.BowlerName);

        var highAverage = bowlerStats
            .Where(stat => stat.TotalGames > 0 && stat.TotalGames >= minimumGames)
            .Select(stat => stat.TotalPinfall * 1m / stat.TotalGames)
            .DefaultIfEmpty(0m)
            .Max();
        var highAverageBowlers = bowlerStats
            .Where(stat => stat.TotalGames > 0 && stat.TotalGames >= minimumGames && stat.TotalPinfall * 1m / stat.TotalGames == highAverage)
            .ToDictionary(stat => stat.BowlerId, stat => stat.BowlerName);

        // Field Match Play Summary

        var highestMatchPlayWinPercentage = bowlerStats.Max(stat =>
            ComputeRawWinRate(stat.MatchPlayWins, stat.MatchPlayLosses)) * 100m;
        var highestMatchPlayWinPercentageBowlers = bowlerStats
            .Where(stat => ComputeRawWinRate(stat.MatchPlayWins, stat.MatchPlayLosses) * 100m == highestMatchPlayWinPercentage)
            .ToDictionary(stat => stat.BowlerId, stat => stat.BowlerName);

        var mostFinals = bowlerStats.Max(stat => stat.Finals);
        var mostFinalsBowlers = bowlerStats
            .Where(stat => stat.Finals == mostFinals)
            .ToDictionary(stat => stat.BowlerId, stat => stat.BowlerName);

        // Award Standings

        var bowlerOfTheYear = ComputeBotyStandings(bowlerStats, bs => bs.BowlerOfTheYearPoints, _ => true);
        var seniorOfTheYear = ComputeBotyStandings(bowlerStats, bs => bs.SeniorOfTheYearPoints, bs => bs.IsSenior);
        var superSeniorOfTheYear = ComputeBotyStandings(bowlerStats, bs => bs.SuperSeniorOfTheYearPoints, bs => bs.IsSuperSenior);
        var womanOfTheYear = ComputeBotyStandings(bowlerStats, bs => bs.WomanOfTheYearPoints, bs => bs.IsWoman);
        var rookieOfTheYear = ComputeBotyStandings(bowlerStats, bs => bs.BowlerOfTheYearPoints, bs => bs.IsRookie);
        var youthOfTheYear = ComputeBotyStandings(bowlerStats, bs => bs.YouthOfTheYearPoints, bs => bs.IsYouth);

        // Bowler Search List

        var bowlerSearchList = bowlerStats
            .OrderBy(bs => bs.BowlerName.LastName)
            .ThenBy(bs => bs.BowlerName.FirstName)
            .Select(bs => new BowlerSearchEntryDto { BowlerId = bs.BowlerId, BowlerName = bs.BowlerName })
            .ToArray();

        // Leaderboards

        var highAverageLeaderboard = bowlerStats
            .Where(bs => bs.TotalGames > 0 && bs.TotalGames >= minimumGames)
            .OrderByDescending(bs => bs.TotalPinfall * 1m / bs.TotalGames)
            .Select(bs => new HighAverageDto
            {
                BowlerId = bs.BowlerId,
                BowlerName = bs.BowlerName,
                Average = Math.Round(bs.TotalPinfall * 1m / bs.TotalGames, 2),
                Games = bs.TotalGames,
                Tournaments = bs.EligibleTournaments,
                FieldAverage = bs.FieldAverage
            })
            .ToArray();

        var highBlockLeaderboard = bowlerStats
            .Where(bs => bs.HighBlock > 0)
            .OrderByDescending(bs => bs.HighBlock)
            .Select(bs => new HighBlockDto
            {
                BowlerId = bs.BowlerId,
                BowlerName = bs.BowlerName,
                HighBlock = bs.HighBlock,
                HighGame = bs.QualifyingHighGame
            })
            .ToArray();

        var minimumMatchPlayGames = minimumTournaments * 2m;

        var matchPlayAverageLeaderboard = bowlerStats
            .Where(bs => bs.MatchPlayGames > 0 && bs.MatchPlayGames >= minimumMatchPlayGames)
            .OrderByDescending(bs => bs.MatchPlayPinfall * 1m / bs.MatchPlayGames)
            .Select(bs => new MatchPlayAverageDto
            {
                BowlerId = bs.BowlerId,
                BowlerName = bs.BowlerName,
                MatchPlayAverage = Math.Round(bs.MatchPlayPinfall * 1m / bs.MatchPlayGames, 2),
                Games = bs.MatchPlayGames,
                Wins = bs.MatchPlayWins,
                Losses = bs.MatchPlayLosses,
                WinPercentage = ComputeWinPercentage(bs.MatchPlayWins, bs.MatchPlayLosses),
                Winnings = bs.TournamentWinnings
            })
            .ToArray();

        var matchPlayRecordLeaderboard = bowlerStats
            .Where(bs => bs.MatchPlayWins + bs.MatchPlayLosses > 0 && bs.MatchPlayWins + bs.MatchPlayLosses >= minimumMatchPlayGames)
            .OrderByDescending(bs => ComputeWinPercentage(bs.MatchPlayWins, bs.MatchPlayLosses))
            .ThenByDescending(bs => bs.MatchPlayWins)
            .Select(bs => new MatchPlayRecordDto
            {
                BowlerId = bs.BowlerId,
                BowlerName = bs.BowlerName,
                Wins = bs.MatchPlayWins,
                Losses = bs.MatchPlayLosses,
                WinPercentage = ComputeWinPercentage(bs.MatchPlayWins, bs.MatchPlayLosses),
                Finals = bs.Finals,
                MatchPlayAverage = bs.MatchPlayGames > 0
                    ? Math.Round(bs.MatchPlayPinfall * 1m / bs.MatchPlayGames, 2)
                    : 0m,
                Winnings = bs.TournamentWinnings
            })
            .ToArray();

        var matchPlayAppearancesLeaderboard = bowlerStats
            .Where(bs => bs.Finals > 0)
            .OrderByDescending(bs => bs.Finals)
            .Select(bs => new MatchPlayAppearancesDto
            {
                BowlerId = bs.BowlerId,
                BowlerName = bs.BowlerName,
                Finals = bs.Finals,
                Tournaments = bs.TotalTournaments,
                Entries = bs.TotalEntries
            })
            .ToArray();

        var pointsPerEntryLeaderboard = bowlerStats
            .Where(bs => bs.EligibleEntries > 0 && bs.EligibleEntries >= minimumEntries && bs.BowlerOfTheYearPoints > 0)
            .OrderByDescending(bs => bs.BowlerOfTheYearPoints * 1m / bs.EligibleEntries)
            .Select(bs => new PointsPerEntryDto
            {
                BowlerId = bs.BowlerId,
                BowlerName = bs.BowlerName,
                PointsPerEntry = Math.Round(bs.BowlerOfTheYearPoints * 1m / bs.EligibleEntries, 2),
                Points = bs.BowlerOfTheYearPoints,
                Entries = bs.EligibleEntries
            })
            .ToArray();

        var pointsPerTournamentLeaderboard = bowlerStats
            .Where(bs => bs.EligibleTournaments > 0 && bs.EligibleTournaments >= minimumTournaments && bs.BowlerOfTheYearPoints > 0)
            .OrderByDescending(bs => bs.BowlerOfTheYearPoints * 1m / bs.EligibleTournaments)
            .Select(bs => new PointsPerTournamentDto
            {
                BowlerId = bs.BowlerId,
                BowlerName = bs.BowlerName,
                Points = bs.BowlerOfTheYearPoints,
                Tournaments = bs.EligibleTournaments,
                PointsPerTournament = Math.Round(bs.BowlerOfTheYearPoints * 1m / bs.EligibleTournaments, 2)
            })
            .ToArray();

        var finalsPerEntryLeaderboard = bowlerStats
            .Where(bs => bs.EligibleEntries > 0 && bs.EligibleEntries >= minimumEntries && bs.Finals > 0)
            .OrderByDescending(bs => bs.Finals * 1m / bs.EligibleEntries)
            .Select(bs => new FinalsPerEntryDto
            {
                BowlerId = bs.BowlerId,
                BowlerName = bs.BowlerName,
                Finals = bs.Finals,
                Entries = bs.EligibleEntries,
                FinalsPerEntry = Math.Round(bs.Finals * 1m / bs.EligibleEntries, 2)
            })
            .ToArray();

        var averageFinishesLeaderboard = bowlerStats
            .Where(bs => bs.AverageFinish.HasValue)
            .OrderBy(bs => bs.AverageFinish!.Value)
            .Select(bs => new AverageFinishDto
            {
                BowlerId = bs.BowlerId,
                BowlerName = bs.BowlerName,
                AverageFinish = bs.AverageFinish!.Value,
                Finals = bs.Finals,
                Winnings = bs.TournamentWinnings
            })
            .ToArray();

        var allBowlers = bowlerStats
            .OrderByDescending(bs => bs.BowlerOfTheYearPoints)
            .Select(bs => new FullStatModalRowDto
            {
                BowlerId = bs.BowlerId,
                BowlerName = bs.BowlerName,
                Points = bs.BowlerOfTheYearPoints,
                Average = bs.TotalGames > 0 ? Math.Round(bs.TotalPinfall * 1m / bs.TotalGames, 2) : 0m,
                Games = bs.TotalGames,
                Finals = bs.Finals,
                Wins = bs.MatchPlayWins,
                Losses = bs.MatchPlayLosses,
                WinPercentage = ComputeWinPercentage(bs.MatchPlayWins, bs.MatchPlayLosses),
                MatchPlayAverage = bs.MatchPlayGames > 0
                    ? Math.Round(bs.MatchPlayPinfall * 1m / bs.MatchPlayGames, 2)
                    : 0m,
                Winnings = bs.TournamentWinnings,
                FieldAverage = bs.FieldAverage,
                Tournaments = bs.TotalTournaments
            })
            .ToArray();

        return new SeasonStatsSummaryDto
        {
            TotalEntries = bowlerStats.Sum(stat => stat.TotalEntries),
            TotalPrizeMoney = bowlerStats.Sum(stat => stat.TournamentWinnings),
            HighGame = highGame,
            HighGameBowlers = highGameBowlers,
            HighBlock = highBlock,
            HighBlockBowlers = highBlockBowlers,
            HighAverage = highAverage,
            HighAverageBowlers = highAverageBowlers,
            HighestMatchPlayWinPercentage = highestMatchPlayWinPercentage,
            HighestMatchPlayWinPercentageBowlers = highestMatchPlayWinPercentageBowlers,
            MostFinals = mostFinals,
            MostFinalsBowlers = mostFinalsBowlers,
            BowlerOfTheYear = bowlerOfTheYear,
            SeniorOfTheYear = seniorOfTheYear,
            SuperSeniorOfTheYear = superSeniorOfTheYear,
            WomanOfTheYear = womanOfTheYear,
            RookieOfTheYear = rookieOfTheYear,
            YouthOfTheYear = youthOfTheYear,
            BowlerSearchList = bowlerSearchList,
            HighAverageLeaderboard = highAverageLeaderboard,
            HighBlockLeaderboard = highBlockLeaderboard,
            MatchPlayAverageLeaderboard = matchPlayAverageLeaderboard,
            MatchPlayRecordLeaderboard = matchPlayRecordLeaderboard,
            MatchPlayAppearancesLeaderboard = matchPlayAppearancesLeaderboard,
            PointsPerEntryLeaderboard = pointsPerEntryLeaderboard,
            PointsPerTournamentLeaderboard = pointsPerTournamentLeaderboard,
            FinalsPerEntryLeaderboard = finalsPerEntryLeaderboard,
            AverageFinishesLeaderboard = averageFinishesLeaderboard,
            AllBowlers = allBowlers
        };
    }

    public async Task<(decimal NumberOfGames, decimal NumberOfTournaments, decimal NumberOfEntries)> GetStatMinimumsForSeasonAsync(SeasonDto season, CancellationToken cancellationToken)
    {
        var tournamentCount = await _tournamentQueries.GetTournamentCountForSeasonAsync(season.Id, cancellationToken);

        return (
            NumberOfGames: tournamentCount * 4.5m,
            NumberOfTournaments: tournamentCount / 2m,
            NumberOfEntries: tournamentCount * .75m
        );
    }

    /// <summary>
    /// This is a temporary method to get the progression of the bowler of the year points race until tournaments and points come into the application. Once that happens, this can be reworked to pull from the database instead of a json file and the temporary file can be deleted.
    /// </summary>
    public Task<IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>> GetBowlerOfTheYearRaceAsync(SeasonDto season, IBowlerQueries bowlerQueries, CancellationToken cancellationToken)
        => _BowlerOfTheYearProgression.GetBowlerOfTheYearProgressionAsync(season, _bowlerQueries, cancellationToken);

    private static IReadOnlyCollection<BowlerOfTheYearStandingDto> ComputeBotyStandings(
        IReadOnlyCollection<BowlerSeasonStatsDto> bowlerStats,
        Func<BowlerSeasonStatsDto, int> pointsSelector,
        Func<BowlerSeasonStatsDto, bool> categoryFilter) =>
        [.. bowlerStats
            .Where(bs => categoryFilter(bs) && pointsSelector(bs) > 0)
            .OrderByDescending(pointsSelector)
            .Select(bs => new BowlerOfTheYearStandingDto
            {
                BowlerId = bs.BowlerId,
                BowlerName = bs.BowlerName,
                Points = pointsSelector(bs),
                Tournaments = bs.EligibleTournaments,
                Entries = bs.EligibleEntries,
                Finals = bs.Finals,
                AverageFinish = bs.AverageFinish,
                Winnings = bs.TournamentWinnings
            })];

    private static decimal ComputeWinPercentage(int wins, int losses)
    {
        var total = wins + losses;
        return total > 0 ? Math.Round(wins * 1m / total * 100, 2) : 0m;
    }

    private static decimal ComputeRawWinRate(int wins, int losses)
    {
        var total = wins + losses;
        return total > 0 ? wins * 1m / total : 0m;
    }
}

internal interface ISeasonStatsService
{
    Task<IReadOnlyCollection<SeasonDto>> GetSeasonsWithStatsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<BowlerSeasonStatsDto>> GetBowlerSeasonStatsAsync(SeasonId seasonId, CancellationToken cancellationToken);

    SeasonStatsSummaryDto CalculateSeasonStatsSummary(
        IReadOnlyCollection<BowlerSeasonStatsDto> bowlerStats,
        decimal minimumGames,
        decimal minimumTournaments,
        decimal minimumEntries);

    /// <summary>
    /// This will take in seasonId when tournaments are in the database, until then it will take in season to pull the minimum
    /// </summary>
    /// <param name="season"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<(decimal NumberOfGames, decimal NumberOfTournaments, decimal NumberOfEntries)> GetStatMinimumsForSeasonAsync(SeasonDto season, CancellationToken cancellationToken);

    /// <summary>
    /// This is a temporary method to get the progression of the bowler of the year points race until tournaments and points come into the application. Once that happens, this can be reworked to pull from the database instead of a json file and the temporary file can be deleted.
    /// </summary>
    Task<IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>> GetBowlerOfTheYearRaceAsync(SeasonDto season, IBowlerQueries bowlerQueries, CancellationToken cancellationToken);
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