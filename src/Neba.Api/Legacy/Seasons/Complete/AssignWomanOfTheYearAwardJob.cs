using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Identity;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Legacy.Seasons.Complete;

internal sealed class AssignWomanOfTheYearAwardJob(
    AppDbContext db, IFusionCache cache, ILogger<AssignWomanOfTheYearAwardJob> logger)
{
    public async Task AssignAsync(SeasonId seasonId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var season = await db.Seasons
            .Include(s => s.BowlerOfTheYearAwards)
            .SingleAsync(s => s.Id == seasonId, ct);

        if (season.BowlerOfTheYearAwards.Any(a => a.Category == BowlerOfTheYearCategory.Woman))
        {
            logger.LogAwardAlreadyAssigned(seasonId, nameof(BowlerOfTheYearCategory.Woman));
            return;
        }

        var stats = await db.BowlerSeasonStats
            .Where(s => s.SeasonId == seasonId && s.IsWoman)
            .ToListAsync(ct);

        if (stats.Count == 0)
        {
            logger.LogNoEligibleCandidatesForCategory(seasonId, nameof(BowlerOfTheYearCategory.Woman));
            return;
        }

        var winners = BowlerSeasonStatsRanking.TopTiedBy(stats, s => s.WomanOfTheYearPoints);
        var genderByBowlerId = await db.Bowlers
            .Where(b => winners.Select(w => w.BowlerId).Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Gender, ct);

        foreach (var winner in winners.Where(winner => genderByBowlerId.GetValueOrDefault(winner.BowlerId) is null))
        {
            logger.LogAwardCandidateMissingBowlerData(seasonId, winner.BowlerId, nameof(BowlerOfTheYearCategory.Woman));
        }

        var failures = winners
            .Where(winner => genderByBowlerId.GetValueOrDefault(winner.BowlerId) is not null)
            .Select(winner => (Winner: winner, Result: season.AddWomanOfTheYearWinner(winner.BowlerId, genderByBowlerId[winner.BowlerId]!)))
            .Where(entry => entry.Result.IsError);

        foreach (var (winner, result) in failures)
        {
            logger.LogAwardAssignmentFailed(seasonId, winner.BowlerId, nameof(BowlerOfTheYearCategory.Woman), result.FirstError.Description);
        }

        await db.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync("neba:awards:bowler-of-the-year", token: ct);
    }
}
