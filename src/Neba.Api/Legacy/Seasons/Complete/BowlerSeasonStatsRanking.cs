using Neba.Api.Features.Stats.Domain;

namespace Neba.Api.Legacy.Seasons.Complete;

internal static class BowlerSeasonStatsRanking
{
    /// <summary>
    /// Every candidate tied for the maximum of <paramref name="selector"/> — empty if
    /// <paramref name="candidates"/> is empty. Ties are intentional: <see cref="Neba.Api.Features.Seasons.Domain.Season"/>'s
    /// own Add*Winner methods already support multiple winners sharing the same value.
    /// </summary>
    public static IReadOnlyCollection<BowlerSeasonStats> TopTiedBy<TValue>(
        IEnumerable<BowlerSeasonStats> candidates,
        Func<BowlerSeasonStats, TValue> selector)
        where TValue : IComparable<TValue>
    {
        var list = candidates.ToList();
        if (list.Count == 0)
        {
            return [];
        }

        var max = list.Max(selector);
        return [.. list.Where(c => selector(c).CompareTo(max) == 0)];
    }
}
