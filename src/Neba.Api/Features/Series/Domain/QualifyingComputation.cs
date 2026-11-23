using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

internal static class QualifyingComputation
{
    // Slots: every result row at Place <= N (ties at N all count; a team event counts rows, so N teams give N x team size).
    // A slot that would go to a non-member, or to a bowler already in the field, stays vacant for the Backfill and is not
    // passed down the finish list. Clear overrides vacate a filled slot. Add overrides add any bowler with a result row
    // and do not fill a vacancy.
    public static StageComputation Compute(
        SeriesStage stage,
        StageTournamentResults tournament,
        IReadOnlySet<BowlerId> alreadyQualified,
        IReadOnlySet<BowlerId> membersOnEventDate,
        IReadOnlyCollection<StageOverride> overrides)
    {
        var advancing = stage.AdvancingCount ?? 0;
        var placeByBowler = tournament.Results.ToDictionary(row => row.BowlerId, row => row.Place);

        var slotRows = tournament.Results
            .Where(row => row.Place <= advancing)
            .OrderBy(row => row.Place).ThenBy(row => row.BowlerId)
            .ToList();

        var clears = overrides.Where(o => o.Kind == OverrideKind.Clear).ToDictionary(o => o.BowlerId);
        var adds = overrides.Where(o => o.Kind == OverrideKind.Add).OrderBy(o => o.CreatedAt).ThenBy(o => o.Id).ToList();

        var entries = new List<FieldEntryDraft>();
        var held = new HashSet<BowlerId>();
        var cleared = new HashSet<BowlerId>();

        foreach (var row in slotRows)
        {
            if (!membersOnEventDate.Contains(row.BowlerId) || alreadyQualified.Contains(row.BowlerId))
            {
                continue; // vacant
            }

            if (clears.ContainsKey(row.BowlerId))
            {
                cleared.Add(row.BowlerId); // vacant by override
                continue;
            }

            entries.Add(new FieldEntryDraft(row.BowlerId, stage.Id, row.Place, null, false));
            held.Add(row.BowlerId);
        }

        var effects = clears.Values.Select(c => new OverrideEffect(c.Id, cleared.Contains(c.BowlerId))).ToList();

        foreach (var add in adds)
        {
            var applies = placeByBowler.TryGetValue(add.BowlerId, out var place)
                && !alreadyQualified.Contains(add.BowlerId)
                && !held.Contains(add.BowlerId);

            if (applies)
            {
                entries.Add(new FieldEntryDraft(add.BowlerId, stage.Id, place, null, true));
                held.Add(add.BowlerId);
            }

            effects.Add(new OverrideEffect(add.Id, applies));
        }

        return new StageComputation
        {
            Entries = entries,
            SlotCount = slotRows.Count,
            Vacancies = slotRows.Count - entries.Count(e => !e.ViaOverride),
            OverrideEffects = effects,
        };
    }
}