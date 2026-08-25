using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Identity;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Legacy.Seasons.Complete;

internal sealed class AssignHighBlockAwardJob(
    AppDbContext db, IFusionCache cache, ILogger<AssignHighBlockAwardJob> logger)
{
    // HighBlock is only ever populated from a legacy qualifying entry whose Games column was
    // exactly 5 (GenerateSeasonStatsJob's inherited Software limitation) — BowlerSeasonStats
    // stores the winning score but not the game count, so 5 is the only value consistent with
    // how HighBlock is ever produced. See the plan's Decision Recap.
    private const int HighBlockGames = 5;

    public async Task AssignAsync(SeasonId seasonId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var season = await db.Seasons
            .Include(s => s.HighBlockAwards)
            .SingleAsync(s => s.Id == seasonId, ct);

        if (season.HighBlockAwards.Count > 0)
        {
            logger.LogAwardAlreadyAssigned(seasonId, "HighBlock");
            return;
        }

        var stats = await db.BowlerSeasonStats
            .Where(s => s.SeasonId == seasonId && s.HighBlock > 0)
            .ToListAsync(ct);

        if (stats.Count == 0)
        {
            logger.LogNoEligibleCandidatesForCategory(seasonId, "HighBlock");
            return;
        }

        var winners = BowlerSeasonStatsRanking.TopTiedBy(stats, s => s.HighBlock);

        foreach (var winner in winners)
        {
            var result = season.AddHighBlockWinner(winner.BowlerId, winner.HighBlock, HighBlockGames);
            if (result.IsError)
            {
                logger.LogAwardAssignmentFailed(seasonId, winner.BowlerId, "HighBlock", result.FirstError.Description);
            }
        }

        await db.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync("neba:awards:high-block", token: ct);
    }
}