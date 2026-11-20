using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

internal static class SurvivorComputation
{
    // Only bowlers in the field can advance, on total over the games they bowled on the designated squad (a partial block
    // counts). All ties at the cut advance. Fewer bowlers than the count: everyone who bowled advances. A bowler who did not
    // bowl is eliminated and no alternate takes the spot.
    public static StageComputation Compute(SeriesStage stage, IReadOnlyCollection<BowlerId> field, StageSquadFact squad)
    {
        var totals = squad.Totals.Where(t => t.Games > 0).ToDictionary(t => t.BowlerId);
        var count = stage.AdvancingCount ?? 0;

        var bowled = field
            .Where(totals.ContainsKey)
            .Select(bowler => (BowlerId: bowler, totals[bowler].Total))
            .OrderByDescending(x => x.Total).ThenBy(x => x.BowlerId)
            .ToList();

        int cutoff;
        if (count <= 0)
        {
            cutoff = int.MaxValue;
        }
        else if (bowled.Count <= count)
        {
            cutoff = int.MinValue;
        }
        else
        {
            cutoff = bowled[count - 1].Total;
        }

        var advanced = bowled.Where(x => x.Total >= cutoff).Select(x => x.BowlerId).ToHashSet();

        return new StageComputation
        {
            Outcomes = [.. field.OrderBy(bowler => bowler).Select(bowler => new SurvivorOutcomeDraft(
                bowler, totals.TryGetValue(bowler, out var t) ? t.Total : null, advanced.Contains(bowler)))],
        };
    }
}