using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Identity;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Legacy.Seasons.Complete;

internal sealed class AssignRookieBowlerOfTheYearAwardJob(
    AppDbContext db, IFusionCache cache, ILogger<AssignRookieBowlerOfTheYearAwardJob> logger)
{
    public async Task AssignAsync(SeasonId seasonId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var season = await db.Seasons
            .Include(s => s.BowlerOfTheYearAwards)
            .SingleAsync(s => s.Id == seasonId, ct);

        if (season.BowlerOfTheYearAwards.Any(a => a.Category == BowlerOfTheYearCategory.Rookie))
        {
            logger.LogAwardAlreadyAssigned(seasonId, nameof(BowlerOfTheYearCategory.Rookie));
            return;
        }

        var stats = await db.BowlerSeasonStats
            .Where(s => s.SeasonId == seasonId && s.IsRookie)
            .ToListAsync(ct);

        if (stats.Count == 0)
        {
            logger.LogNoEligibleCandidatesForCategory(seasonId, nameof(BowlerOfTheYearCategory.Rookie));
            return;
        }

        // No dedicated RookieOfTheYearPoints column exists — ranked by the same
        // BowlerOfTheYearPoints as Open, filtered to IsRookie. See the plan's Decision Recap.
        var winners = BowlerSeasonStatsRanking.TopTiedBy(stats, s => s.BowlerOfTheYearPoints);

        var failures = winners
            .Select(winner => (Winner: winner, Result: season.AddRookieBowlerOfTheYearWinner(winner.BowlerId, isRookie: true)))
            .Where(entry => entry.Result.IsError);

        foreach (var (winner, result) in failures)
        {
            logger.LogAwardAssignmentFailed(seasonId, winner.BowlerId, nameof(BowlerOfTheYearCategory.Rookie), result.FirstError.Description);
        }

        await db.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync("neba:awards:bowler-of-the-year", token: ct);
    }
}
