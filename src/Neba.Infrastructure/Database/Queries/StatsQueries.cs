using Microsoft.EntityFrameworkCore;

using Neba.Application.Stats;
using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Seasons;
using Neba.Domain.Stats;

namespace Neba.Infrastructure.Database.Queries;

internal sealed class StatsQueries(AppDbContext appDbContext)
    : IStatsQueries
{
    private readonly IQueryable<BowlerSeasonStats> _bowlerSeasonStats
        = appDbContext.BowlerSeasonStats.AsNoTracking();

    public async Task<IReadOnlyCollection<BowlerSeasonStatsDto>> GetBowlerSeasonStatsAsync(SeasonId seasonId, CancellationToken cancellationToken)
        => await _bowlerSeasonStats
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

    public async Task<IDictionary<SeasonId, string>> GetSeasonsWithStatsAsync(CancellationToken cancellationToken)
        => await _bowlerSeasonStats.
            Select(stat => new { stat.SeasonId, stat.Season.Description })
            .Distinct()
            .ToDictionaryAsync(x => x.SeasonId, x => x.Description, cancellationToken);
}