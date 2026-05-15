using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using Neba.Api.Caching;
using Neba.Api.Database;
using Neba.Api.Database.Entities;
using Neba.Api.Features.Stats.BoyProgression;
using Neba.Api.Features.Stats.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Stats.GetSeasonStats;

internal sealed class GetSeasonStatsQueryHandler(
    AppDbContext appDbContext,
    ISeasonStatsCalculator seasonStatsCalculator,
    IBowlerOfTheYearRaceCalculator bowlerOfTheYearRaceCalculator,
    HybridCache cache)
        : IQueryHandler<GetSeasonStatsQuery, ErrorOr<SeasonStatsDto>>
{
    private readonly IQueryable<Tournament> _tournaments
        = appDbContext.Tournaments.AsNoTracking();
    private readonly IQueryable<BowlerSeasonStats> _bowlerSeasonStats
        = appDbContext.BowlerSeasonStats.AsNoTracking();
    private readonly IQueryable<HistoricalTournamentResult> _historicalTournamentResults
        = appDbContext.HistoricalTournamentResults.AsNoTracking();

    private readonly ISeasonStatsCalculator _seasonStatsCalculator = seasonStatsCalculator;
    private readonly IBowlerOfTheYearRaceCalculator _bowlerOfTheYearRaceCalculator = bowlerOfTheYearRaceCalculator;
    private readonly HybridCache _cache = cache;

    public async Task<ErrorOr<SeasonStatsDto>> HandleAsync(GetSeasonStatsQuery query, CancellationToken cancellationToken)
    {
        var seasonsWithStats = await _bowlerSeasonStats
            .Select(stat => new SeasonWithStatsDto
            {
                Id = stat.Season.Id,
                Description = stat.Season.Description,
                StartDate = stat.Season.StartDate,
                EndDate = stat.Season.EndDate
            }).ToListAsync(cancellationToken);

        var season = query.SeasonYear.HasValue
            ? seasonsWithStats.FirstOrDefault(s => s.EndDate.Year == query.SeasonYear.Value || s.StartDate.Year == query.SeasonYear.Value)
            : seasonsWithStats.OrderByDescending(s => s.EndDate).FirstOrDefault();

        if (season is null)
        {
            return StatsErrors.SeasonHasNoStats;
        }

        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(season.Id);

        return await _cache.GetOrCreateAsync(
             descriptor.Key,
             async cancel => await ComputeSeasonStatsAsync(season.Id, seasonsWithStats, cancel),
             tags: descriptor.Tags,
             cancellationToken: cancellationToken);
    }

    private async Task<SeasonStatsDto> ComputeSeasonStatsAsync(
        SeasonId seasonId,
        List<SeasonWithStatsDto> seasonsWithStats,
        CancellationToken cancellationToken)
    {
        var season = seasonsWithStats.First(s => s.Id == seasonId);

        var tournamentCount = await _tournaments.CountAsync(tournament => tournament.SeasonId == seasonId, cancellationToken);

        var (numberOfGames, numberOfTournaments, numberOfEntries) = _seasonStatsCalculator.CalculateStatMinimums(tournamentCount);

        var bowlerStats = await _bowlerSeasonStats
            .Where(stat => stat.SeasonId == seasonId)
            .Select(stat => new BowlerSeasonStatsDto
            {
                BowlerId = stat.BowlerId,
                BowlerName = stat.Bowler.Name,
                IsMember = stat.IsMember,
                IsRookie = stat.IsRookie,
                IsSenior = stat.IsSenior,
                IsSuperSenior = stat.IsSuperSenior,
                IsWoman = stat.IsWoman,
                IsYouth = stat.IsYouth,
                EligibleTournaments = stat.EligibleTournaments,
                TotalTournaments = stat.TotalTournaments,
                EligibleEntries = stat.EligibleEntries,
                TotalEntries = stat.TotalEntries,
                Cashes = stat.Cashes,
                Finals = stat.Finals,
                QualifyingHighGame = stat.QualifyingHighGame,
                HighBlock = stat.HighBlock,
                MatchPlayWins = stat.MatchPlayWins,
                MatchPlayLosses = stat.MatchPlayLosses,
                MatchPlayGames = stat.MatchPlayGames,
                MatchPlayPinfall = stat.MatchPlayPinfall,
                MatchPlayHighGame = stat.MatchPlayHighGame,
                TotalGames = stat.TotalGames,
                TotalPinfall = stat.TotalPinfall,
                FieldAverage = stat.FieldAverage,
                HighFinish = stat.HighFinish,
                AverageFinish = stat.AverageFinish,
                BowlerOfTheYearPoints = stat.BowlerOfTheYearPoints,
                SeniorOfTheYearPoints = stat.SeniorOfTheYearPoints,
                SuperSeniorOfTheYearPoints = stat.SuperSeniorOfTheYearPoints,
                WomanOfTheYearPoints = stat.WomanOfTheYearPoints,
                YouthOfTheYearPoints = stat.YouthOfTheYearPoints,
                TournamentWinnings = stat.TournamentWinnings,
                CupEarnings = stat.CupEarnings,
                Credits = stat.Credits,
                LastUpdatedUtc = stat.LastUpdatedUtc
            }).ToListAsync(cancellationToken);

        var bowlerOfTheYearProgressions = await _historicalTournamentResults
            .Where(result => result.Tournament.SeasonId == seasonId)
            .OrderBy(result => result.Tournament.StartDate)
            .Select(result => new BoyProgressionResultDto
            {
                BowlerId = result.Bowler.Id,
                BowlerName = result.Bowler.Name,
                BowlerDateOfBirth = result.Bowler.DateOfBirth,
                BowlerGender = result.Bowler.Gender == null
                    ? null
                    : result.Bowler.Gender.Value,
                TournamentId = result.Tournament.Id,
                TournamentName = result.Tournament.Name,
                TournamentDate = result.Tournament.StartDate,
                TournamentEndDate = result.Tournament.EndDate,
                StatsEligible = result.Tournament.StatsEligible,
                TournamentType = result.Tournament.TournamentType.Value,
                Points = result.Points,
                SideCutId = result.SideCutId,
                SideCutName = result.SideCut != null
                    ? result.SideCut.Name
                    : null
            }).ToListAsync(cancellationToken);

        var bowlerOfTheYearRaces = _bowlerOfTheYearRaceCalculator.CalculateAllProgressions(bowlerOfTheYearProgressions);

        var seasonSummary = _seasonStatsCalculator.CalculateSeasonStatsSummary(
            bowlerStats, numberOfGames, numberOfTournaments, numberOfEntries);

        return new SeasonStatsDto
        {
            Season = season,
            SeasonsWithStats = [.. seasonsWithStats.OrderByDescending(s => s.EndDate)],
            BowlerStats = bowlerStats,
            BowlerOfTheYearRaces = bowlerOfTheYearRaces,
            Summary = seasonSummary,
            MinimumNumberOfGames = numberOfGames,
            MinimumNumberOfTournaments = numberOfTournaments,
            MinimumNumberOfEntries = numberOfEntries
        };
    }
}