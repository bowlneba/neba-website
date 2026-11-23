using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

internal static class BackfillComputation
{
    // Fills vacancies from open Bowler of the Year points as of the stored date. Candidates must have points above zero, be
    // members on the as-of date, not be in the field, and not be barred by an applied Clear override. Ties at the cutoff all
    // advance; with fewer candidates than vacancies, every candidate advances and the rest stay vacant.
    public static IReadOnlyList<FieldEntryDraft> Compute(
        int vacancies,
        IReadOnlySet<BowlerId> inField,
        IReadOnlySet<BowlerId> barred,
        IReadOnlySet<BowlerId> membersAsOf,
        IReadOnlyList<BowlerPointsFact> points)
    {
        if (vacancies <= 0)
        {
            return [];
        }

        var candidates = points
            .Where(p => p.Points > 0 && membersAsOf.Contains(p.BowlerId) && !inField.Contains(p.BowlerId) && !barred.Contains(p.BowlerId))
            .OrderByDescending(p => p.Points).ThenBy(p => p.BowlerId)
            .ToList();

        if (candidates.Count == 0)
        {
            return [];
        }

        var cutoff = candidates.Count <= vacancies ? candidates[^1].Points : candidates[vacancies - 1].Points;

        return [.. candidates
            .Where(p => p.Points >= cutoff)
            .Select(p => new FieldEntryDraft(p.BowlerId, null, null, p.Points, false))];
    }
}