using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Identity;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Legacy.Seasons.Complete;

internal sealed class AssignBowlerOfTheYearAwardJob(
    AppDbContext db, IFusionCache cache, ILogger<AssignBowlerOfTheYearAwardJob> logger)
{
    public async Task AssignAsync(SeasonId seasonId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var season = await db.Seasons
            .Include(s => s.BowlerOfTheYearAwards)
            .SingleAsync(s => s.Id == seasonId, ct);

        if (season.BowlerOfTheYearAwards.Any(a => a.Category == BowlerOfTheYearCategory.Open))
        {
            logger.LogAwardAlreadyAssigned(seasonId, nameof(BowlerOfTheYearCategory.Open));
            return;
        }

        var stats = await db.BowlerSeasonStats.Where(s => s.SeasonId == seasonId).ToListAsync(ct);
        if (stats.Count == 0)
        {
            logger.LogNoBowlerSeasonStatsForSeason(seasonId);
            return;
        }

        var winners = BowlerSeasonStatsRanking.TopTiedBy(stats, s => s.BowlerOfTheYearPoints);

        var failures = winners
            .Select(winner => (Winner: winner, Result: season.AddBowlerOfTheYearWinner(winner.BowlerId)))
            .Where(entry => entry.Result.IsError);

        foreach (var (winner, result) in failures)
        {
            logger.LogAwardAssignmentFailed(seasonId, winner.BowlerId, nameof(BowlerOfTheYearCategory.Open), result.FirstError.Description);
        }

        await db.SaveChangesAsync(ct);

        // Shared tag: ListBowlerOfTheYearAwardsQuery returns one combined list across all six
        // categories, so any category's job writing a winner must evict the same tag.
        await cache.RemoveByTagAsync("neba:awards:bowler-of-the-year", token: ct);
    }
}
