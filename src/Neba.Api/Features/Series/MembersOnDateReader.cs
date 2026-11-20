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
}