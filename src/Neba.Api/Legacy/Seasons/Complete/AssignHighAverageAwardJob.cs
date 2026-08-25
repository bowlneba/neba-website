using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Identity;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Legacy.Seasons.Complete;

internal sealed class AssignHighAverageAwardJob(
    AppDbContext db, IFusionCache cache, ILogger<AssignHighAverageAwardJob> logger)
{
    private const decimal MinimumGamesMultiplier = 4.5m;

    public async Task AssignAsync(SeasonId seasonId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var season = await db.Seasons
            .Include(s => s.HighAverageAwards)
            .SingleAsync(s => s.Id == seasonId, ct);

        if (season.HighAverageAwards.Count > 0)
        {
            logger.LogAwardAlreadyAssigned(seasonId, "HighAverage");
            return;
        }

        // Season-wide constant — every bowler's minimum-games bar uses the same count of the
        // season's own stat-eligible tournaments, not each bowler's personal EligibleTournaments.
        // See the plan's Decision Recap: "statEligibleTournamentCount ... is a season-wide constant".
        var statEligibleTournamentCount = await db.Tournaments
            .CountAsync(t => t.SeasonId == seasonId && t.StatsEligible, ct);
        var minimumGames = (int)Math.Floor(MinimumGamesMultiplier * statEligibleTournamentCount);

        var stats = await db.BowlerSeasonStats
            .Where(s => s.SeasonId == seasonId && s.TotalGames >= minimumGames && s.TotalGames > 0)
            .ToListAsync(ct);

        if (stats.Count == 0)
        {
            logger.LogNoEligibleCandidatesForCategory(seasonId, "HighAverage");
            return;
        }

        var winners = BowlerSeasonStatsRanking.TopTiedBy(stats, s => s.TotalPinfall / (decimal)s.TotalGames);

        foreach (var winner in winners)
        {
            var average = winner.TotalPinfall / (decimal)winner.TotalGames;
            var result = season.AddHighAverageWinner(
                winner.BowlerId, average, winner.TotalGames, winner.TotalTournaments, statEligibleTournamentCount);

            if (result.IsError)
            {
                logger.LogAwardAssignmentFailed(seasonId, winner.BowlerId, "HighAverage", result.FirstError.Description);
            }
        }

        await db.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync("neba:awards:high-average", token: ct);
    }
}