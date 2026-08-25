using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Identity;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Legacy.Seasons.Complete;

internal sealed class AssignYouthBowlerOfTheYearAwardJob(
    AppDbContext db, IFusionCache cache, ILogger<AssignYouthBowlerOfTheYearAwardJob> logger)
{
    public async Task AssignAsync(SeasonId seasonId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var season = await db.Seasons
            .Include(s => s.BowlerOfTheYearAwards)
            .SingleAsync(s => s.Id == seasonId, ct);

        if (season.BowlerOfTheYearAwards.Any(a => a.Category == BowlerOfTheYearCategory.Youth))
        {
            logger.LogAwardAlreadyAssigned(seasonId, nameof(BowlerOfTheYearCategory.Youth));
            return;
        }

        var stats = await db.BowlerSeasonStats
            .Where(s => s.SeasonId == seasonId && s.IsYouth)
            .ToListAsync(ct);

        if (stats.Count == 0)
        {
            logger.LogNoEligibleCandidatesForCategory(seasonId, nameof(BowlerOfTheYearCategory.Youth));
            return;
        }

        var winners = BowlerSeasonStatsRanking.TopTiedBy(stats, s => s.YouthOfTheYearPoints);
        var dobByBowlerId = await db.Bowlers
            .Where(b => winners.Select(w => w.BowlerId).Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.DateOfBirth, ct);

        var agesByBowlerId = winners.ToDictionary(
            winner => winner.BowlerId,
            winner => SeasonAgeCalculator.AgeOnDate(dobByBowlerId.GetValueOrDefault(winner.BowlerId), season.EndDate));

        foreach (var winner in winners.Where(winner => agesByBowlerId[winner.BowlerId] is null))
        {
            logger.LogAwardCandidateMissingBowlerData(seasonId, winner.BowlerId, nameof(BowlerOfTheYearCategory.Youth));
        }

        var failures = winners
            .Where(winner => agesByBowlerId[winner.BowlerId] is not null)
            .Select(winner => (Winner: winner, Result: season.AddYouthBowlerOfTheYearWinner(winner.BowlerId, agesByBowlerId[winner.BowlerId]!.Value)))
            .Where(entry => entry.Result.IsError);

        foreach (var (winner, result) in failures)
        {
            logger.LogAwardAssignmentFailed(seasonId, winner.BowlerId, nameof(BowlerOfTheYearCategory.Youth), result.FirstError.Description);
        }

        await db.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync("neba:awards:bowler-of-the-year", token: ct);
    }
}
