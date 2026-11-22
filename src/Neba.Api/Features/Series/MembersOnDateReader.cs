using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series;

internal static class MembersOnDateReader
{
    /// <summary>
    /// Bowlers with a membership that covers <paramref name="date"/> (both ends inclusive). About a thousand
    /// members, so it reads them all. The sync never carries the legacy System Import record, so nothing to skip.
    /// </summary>
    public static async Task<IReadOnlySet<BowlerId>> ReadAsync(
        AppDbContext appDbContext, DateOnly date, CancellationToken cancellationToken)
    {
        var bowlerIds = await appDbContext.Bowlers.AsNoTracking()
            .Where(bowler => bowler.Memberships.Any(membership => membership.StartDate <= date && date <= membership.EndDate))
            .Select(bowler => bowler.Id)
            .ToListAsync(cancellationToken);

        return bowlerIds.ToHashSet();
    }

    /// <summary>
    /// The same read for several dates in one query. It fetches the memberships that overlap the
    /// earliest-to-latest range, then checks each date here.
    /// </summary>
    public static async Task<IReadOnlyDictionary<DateOnly, IReadOnlySet<BowlerId>>> ReadForDatesAsync(
        AppDbContext appDbContext, IReadOnlyCollection<DateOnly> dates, CancellationToken cancellationToken)
    {
        if (dates.Count == 0)
        {
            return new Dictionary<DateOnly, IReadOnlySet<BowlerId>>();
        }

        var earliest = dates.Min();
        var latest = dates.Max();

        var memberships = await appDbContext.Bowlers.AsNoTracking()
            .SelectMany(bowler => bowler.Memberships, (bowler, membership) => new { bowler.Id, membership.StartDate, membership.EndDate })
            .Where(row => row.StartDate <= latest && earliest <= row.EndDate)
            .ToListAsync(cancellationToken);
        return dates.Distinct().ToDictionary(
            date => date,
            date => (IReadOnlySet<BowlerId>)memberships
                .Where(row => row.StartDate <= date && date <= row.EndDate)
                .Select(row => row.Id)
                .ToHashSet());
    }
}
