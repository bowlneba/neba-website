using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.Domain;

public sealed partial class Series
{
    // Whether a Fold leaves Official Stages alone or recomputes them.
    private enum FoldMode
    {
        KeepOfficial,
        ResetOfficial,
    }

    private sealed class FoldTrace
    {
        public bool Changed { get; set; }
        public bool StatusChanged { get; set; }
        public StageOverrideId? OverrideId { get; set; }
        public List<SeriesStageId> Computed { get; } = [];
        public List<SeriesStageId> Stale { get; } = [];
        public List<SeriesStageId> Official { get; } = [];
    }

    /// <summary>The last Qualifying Stage. The Backfill belongs to it.</summary>
    public SeriesStage? LastQualifyingStage
        => Ordered().LastOrDefault(stage => stage.Kind == StageKind.Qualifying);

    /// <summary>True when a Survivor Stage resolved to the squad.</summary>
    public bool HasStageResolvedTo(SquadId squadId)
        => _stages.Exists(stage => stage.Kind == StageKind.Survivor && stage.SquadDesignation?.SquadId == squadId);

    // ---------- entry points ----------

    /// <summary>
    /// Event entry. Re-evaluates the Stage linked to the tournament and every later Stage in order: it resolves a Survivor
    /// squad, computes a Pending Stage that may compute, replaces a Computed Stage's output when it changed, and marks an
    /// Official Stage Stale when a dry compute differs (it never rewrites it). Writing nothing leaves <see cref="Version"/> alone.
    /// </summary>
    public AdvanceResult Advance(TournamentId tournamentId, AdvancementFacts facts, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var stage = _stages.Find(s => s.TournamentId == tournamentId);
        if (stage is null)
        {
            return AdvanceResult.None;
        }

        var trace = new FoldTrace();
        Fold(stage.Order, FoldMode.KeepOfficial, facts, now, trace);

        return Finish(trace);
    }

    /// <summary>
    /// Forced fold from a Stage to the end. Official Stages in the range need <paramref name="confirmResetOfficial"/>; they
    /// reset to Computed and lose Stale. A recompute that changes nothing writes nothing.
    /// </summary>
    public ErrorOr<AdvanceResult> Recompute(
        SeriesStageId fromStageId, bool confirmResetOfficial, int expectedVersion, AdvancementFacts facts, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var stage = _stages.Find(s => s.Id == fromStageId);
        if (stage is null)
        {
            return SeriesAdvancementErrors.StageNotFound(fromStageId);
        }

        var version = EnsureVersion(expectedVersion);
        if (version.IsError)
        {
            return version.Errors;
        }

        var officialOrders = _stages
            .Where(s => s.Order >= stage.Order && s.Status == StageStatus.Official)
            .Select(s => s.Order).Order().ToList();

        if (officialOrders.Count > 0 && !confirmResetOfficial)
        {
            return SeriesAdvancementErrors.ConfirmationRequired(officialOrders);
        }

        var trace = new FoldTrace();
        Fold(stage.Order, FoldMode.ResetOfficial, facts, now, trace);

        return Finish(trace);
    }

    /// <summary>
    /// Makes every Computed Qualifying or Survivor Stage whose grace period passed Official, in order. A Stage waits for every
    /// earlier one, is skipped while stale, and the last Qualifying Stage waits for the Backfill. A Placement Stage never becomes Official.
    /// </summary>
    public AdvanceResult MakeDueOfficial(DateTimeOffset now, TimeSpan grace)
    {
        var trace = new FoldTrace();

        foreach (var stage in Ordered().Where(s => s.Kind != StageKind.Placement))
        {
            if (stage.Status == StageStatus.Official)
            {
                continue;
            }

            if (OfficialDueAt(stage, grace) is not { } due || due > now)
            {
                break;
            }

            stage.MarkOfficial(now);
            trace.Changed = true;
            trace.StatusChanged = true;
            trace.Official.Add(stage.Id);
        }

        return Finish(trace);
    }

    /// <summary>When the Stage will become Official, or null. See <see cref="SeriesAdvancementRules.OfficialDueAt"/>.</summary>
    public DateTimeOffset? OfficialDueAt(SeriesStage stage, TimeSpan grace)
    {
        ArgumentNullException.ThrowIfNull(stage);

        return SeriesAdvancementRules.OfficialDueAt(
            stage.Kind, stage.Status, stage.IsStale, stage.ComputedAt,
            awaitingBackfill: LastQualifyingStage?.Id == stage.Id && BackfillAsOf is null,
            grace);
    }

    // ---------- squad ----------

    /// <summary>
    /// Sets the Survivor Stage's squad, then re-evaluates from that Stage. Allowed in any status: an Official Stage that
    /// lost its squad needs this before an admin recompute can reset it.
    /// </summary>
    public ErrorOr<AdvanceResult> PickSquad(
        SeriesStageId stageId, SquadId squadId, int expectedVersion, AdvancementFacts facts, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var stage = _stages.Find(s => s.Id == stageId);
        if (stage is null)
        {
            return SeriesAdvancementErrors.StageNotFound(stageId);
        }

        var version = EnsureVersion(expectedVersion);
        if (version.IsError)
        {
            return version.Errors;
        }

        if (stage.Kind != StageKind.Survivor)
        {
            return SeriesAdvancementErrors.StageNotSurvivor;
        }

        if (!facts.Tournaments.TryGetValue(stage.TournamentId, out var tournament)
            || !tournament.Squads.Any(s => s.SquadId == squadId))
        {
            return SeriesAdvancementErrors.SquadNotInTournament;
        }

        if (stage.SquadDesignation?.SquadId == squadId)
        {
            return AdvanceResult.None;
        }

        stage.ResolveSquad(squadId);

        var trace = new FoldTrace { Changed = true };
        Fold(stage.Order, FoldMode.KeepOfficial, facts, now, trace);

        return Finish(trace);
    }

    // ---------- overrides ----------

    /// <summary>Adds a bowler who has a result row in the Stage's tournament, then recomputes inline.</summary>
    public ErrorOr<AdvanceResult> AddOverride(
        SeriesStageId stageId, BowlerId bowlerId, string reason, string createdBy,
        int expectedVersion, AdvancementFacts facts, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var target = OverrideTarget(stageId, expectedVersion);
        if (target.IsError)
        {
            return target.Errors;
        }

        var stage = target.Value;

        var hasResultRow = facts.Tournaments.TryGetValue(stage.TournamentId, out var tournament)
            && tournament.Results.Any(row => row.BowlerId == bowlerId);

        return (hasResultRow, _fieldEntries.Exists(e => e.BowlerId == bowlerId), stage.Overrides.Any(o => o.BowlerId == bowlerId)) switch
        {
            (false, _, _) => SeriesAdvancementErrors.NoResultRow,
            (_, true, _) => SeriesAdvancementErrors.BowlerAlreadyInField,
            (_, _, true) => SeriesAdvancementErrors.OverrideExists,
            _ => ApplyOverride(stage, OverrideKind.Add, bowlerId, reason, createdBy, facts, now),
        };
    }

    /// <summary>Vacates the slot a bowler holds in the Stage and bars the bowler from the Backfill, then recomputes inline.</summary>
    public ErrorOr<AdvanceResult> ClearSlot(
        SeriesStageId stageId, BowlerId bowlerId, string reason, string createdBy,
        int expectedVersion, AdvancementFacts facts, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var target = OverrideTarget(stageId, expectedVersion);
        if (target.IsError)
        {
            return target.Errors;
        }

        var stage = target.Value;

        var holdsSlot = _fieldEntries.Exists(e => e.SourceStageId == stage.Id && !e.ViaOverride && e.BowlerId == bowlerId);

        return (stage.Overrides.Any(o => o.BowlerId == bowlerId), holdsSlot) switch
        {
            (true, _) => SeriesAdvancementErrors.OverrideExists,
            (_, false) => SeriesAdvancementErrors.BowlerNotInSlot,
            _ => ApplyOverride(stage, OverrideKind.Clear, bowlerId, reason, createdBy, facts, now),
        };
    }

    /// <summary>
    /// Removes an override and recomputes inline. A missing override returns success before the version check, so a retry
    /// never conflicts.
    /// </summary>
    public ErrorOr<AdvanceResult> RemoveOverride(
        SeriesStageId stageId, StageOverrideId overrideId, int expectedVersion, AdvancementFacts facts, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var stage = _stages.Find(s => s.Id == stageId);
        if (stage is null)
        {
            return SeriesAdvancementErrors.StageNotFound(stageId);
        }

        var existing = stage.Overrides.FirstOrDefault(o => o.Id == overrideId);
        if (existing is null)
        {
            return AdvanceResult.None;
        }

        var target = OverrideTarget(stageId, expectedVersion);
        if (target.IsError)
        {
            return target.Errors;
        }

        stage.RemoveOverride(existing);

        var trace = new FoldTrace { Changed = true };
        Fold(stage.Order, FoldMode.KeepOfficial, facts, now, trace);

        return Finish(trace);
    }

    private ErrorOr<AdvanceResult> ApplyOverride(
        SeriesStage stage, OverrideKind kind, BowlerId bowlerId, string reason, string createdBy,
        AdvancementFacts facts, DateTimeOffset now)
    {
        var created = StageOverride.Create(kind, bowlerId, reason, createdBy, now);
        if (created.IsError)
        {
            return created.Errors;
        }

        stage.AddOverride(created.Value);

        var trace = new FoldTrace { Changed = true, OverrideId = created.Value.Id };
        Fold(stage.Order, FoldMode.KeepOfficial, facts, now, trace);

        return Finish(trace);
    }

    // Checks in order: the Stage exists (404), the version matches (409), the Stage is Qualifying (422), the Stage is Computed (409).
    private ErrorOr<SeriesStage> OverrideTarget(SeriesStageId stageId, int expectedVersion)
    {
        var stage = _stages.Find(s => s.Id == stageId);
        if (stage is null)
        {
            return SeriesAdvancementErrors.StageNotFound(stageId);
        }

        var version = EnsureVersion(expectedVersion);

        return version.IsError
            ? version.Errors
            : stage switch
            {
                _ when stage.Kind != StageKind.Qualifying => SeriesAdvancementErrors.NotQualifyingStage,
                _ when stage.Status != StageStatus.Computed => SeriesAdvancementErrors.StageNotComputed(stage.Status),
                _ => stage,
            };
    }

    // ---------- the fold ----------

    private List<SeriesStage> Ordered()
        => [.. _stages.OrderBy(stage => stage.Order)];

    private AdvanceResult Finish(FoldTrace trace)
    {
        if (trace.Changed)
        {
            Version++;
        }

        return new AdvanceResult
        {
            Changed = trace.Changed,
            StatusChanged = trace.StatusChanged,
            ComputedStageIds = trace.Computed,
            StaleStageIds = trace.Stale,
            OfficialStageIds = trace.Official,
            OverrideId = trace.OverrideId,
            Version = Version,
        };
    }

    // Walks the Stages from fromOrder in order. Each Stage reads the stored output of the Stages before it, which this
    // walk keeps current, so the result is a function of earlier output, results, overrides and facts.
    private void Fold(int fromOrder, FoldMode mode, AdvancementFacts facts, DateTimeOffset now, FoldTrace trace)
    {
        var ordered = Ordered();
        var previous = ordered.LastOrDefault(stage => stage.Order < fromOrder);

        foreach (var stage in ordered.Where(stage => stage.Order >= fromOrder))
        {
            if (!EvaluateStage(stage, previous, mode, facts, now, trace))
            {
                return; // a Pending Stage that cannot compute leaves every later Stage Pending
            }

            previous = stage;
        }
    }

    // Returns false when the walk must stop.
    private bool EvaluateStage(
        SeriesStage stage, SeriesStage? previous, FoldMode mode, AdvancementFacts facts, DateTimeOffset now, FoldTrace trace)
    {
        facts.Tournaments.TryGetValue(stage.TournamentId, out var tournament);

        if (stage.Kind == StageKind.Survivor)
        {
            ResolveSquad(stage, tournament, trace);
        }

        if (!MayCompute(stage, previous, tournament))
        {
            if (stage.Status == StageStatus.Pending)
            {
                return false;
            }

            MarkStale(stage, trace); // stored output can no longer be reproduced; it stays as it is
            return true;
        }

        var computation = ComputeStage(stage, facts, tournament!);
        var same = SameOutput(stage, computation);

        if (stage.Status == StageStatus.Official && mode == FoldMode.KeepOfficial)
        {
            if (!same)
            {
                MarkStale(stage, trace);
            }

            return true;
        }

        if (stage.Status == StageStatus.Pending || !same || (mode == FoldMode.ResetOfficial && stage.Status == StageStatus.Official))
        {
            var statusBefore = stage.Status;

            ApplyComputation(stage, computation);
            stage.MarkComputed(now);

            trace.Changed = true;
            trace.StatusChanged |= statusBefore != StageStatus.Computed;
            trace.Computed.Add(stage.Id);
        }
        else if (stage.IsStale)
        {
            stage.ClearStale();
            trace.Changed = true;
        }

        return true;
    }

    // A Stage may compute when its tournament is Completed or Truncated with at least one result row, the previous Stage is
    // not Pending, a non-Qualifying Stage has the Backfill behind it, and a Survivor Stage has a resolved squad that exists
    // and has at least one score row.
    private bool MayCompute(SeriesStage stage, SeriesStage? previous, StageTournamentResults? tournament)
    {
        return tournament?.Results.Count > 0
            && (tournament.Status == TournamentStatus.Completed || tournament.Status == TournamentStatus.Truncated)
            && (previous is null || previous.Status != StageStatus.Pending)
            && (stage.Kind == StageKind.Qualifying || BackfillAsOf is not null)
            && (stage.Kind != StageKind.Survivor
                || (stage.SquadDesignation?.SquadId is { } squadId
                    && tournament.Squads.FirstOrDefault(s => s.SquadId == squadId) is { Totals.Count: > 0 }));
    }

    private static void ResolveSquad(SeriesStage stage, StageTournamentResults? tournament, FoldTrace trace)
    {
        if (stage.SquadDesignation is not { } designation || tournament is null)
        {
            return;
        }

        var wasResolved = designation.SquadId is not null;

        if (designation.SquadId is { } current)
        {
            if (tournament.Squads.Any(s => s.SquadId == current))
            {
                return;
            }

            stage.UnresolveSquad(); // the squad vanished; fall through in case one with the same date and time exists now
            trace.Changed = true;
        }

        var matches = tournament.Squads
            .Where(s => s.LocalDate == designation.Date && ToMinute(s.LocalStartTime) == ToMinute(designation.StartTime))
            .ToList();

        if (matches.Count == 1)
        {
            stage.ResolveSquad(matches[0].SquadId);
            trace.Changed = true;
        }
        else if (wasResolved)
        {
            MarkStale(stage, trace);
        }
    }

    private static TimeOnly ToMinute(TimeOnly time)
        => new(time.Hour, time.Minute);

    private static void MarkStale(SeriesStage stage, FoldTrace trace)
    {
        if (stage.IsStale || stage.Status == StageStatus.Pending)
        {
            return;
        }

        stage.MarkStale();
        trace.Changed = true;
        trace.Stale.Add(stage.Id);
    }

    // ---------- compute ----------

    private StageComputation ComputeStage(SeriesStage stage, AdvancementFacts facts, StageTournamentResults tournament)
    {
        if (stage.Kind == StageKind.Qualifying)
        {
            var qualifying = QualifyingComputation.Compute(
                stage, tournament, BowlersQualifiedBefore(stage.Order), facts.MembersOn(tournament.EndDate), stage.Overrides);

            return LastQualifyingStage?.Id == stage.Id
                ? WithBackfill(stage, qualifying, facts, tournament)
                : qualifying;
        }

        if (stage.Kind == StageKind.Survivor)
        {
            var squad = tournament.Squads.Single(s => s.SquadId == stage.SquadDesignation!.SquadId!.Value);

            return SurvivorComputation.Compute(stage, FieldEnteringStage(stage), squad);
        }

        return PlacementComputation.Compute(FieldEnteringStage(stage), tournament, facts.SortNames, Finale?.FinalistCount);
    }

    private StageComputation WithBackfill(
        SeriesStage stage, StageComputation qualifying, AdvancementFacts facts, StageTournamentResults tournament)
    {
        var asOf = tournament.EndDate;

        var inField = BowlersQualifiedBefore(stage.Order);
        inField.UnionWith(qualifying.Entries.Select(e => e.BowlerId));

        var earlier = Ordered().Where(s => s.Kind == StageKind.Qualifying && s.Order < stage.Order).ToList();
        var vacancies = qualifying.Vacancies + earlier.Sum(VacanciesOf);

        if (vacancies == 0)
        {
            return qualifying with { BackfillAsOf = asOf }; // nothing to fill, so the Backfill has "run"
        }

        if (facts.Backfill is not { Ready: true } backfill || backfill.AsOf != asOf)
        {
            // Not ready. A Backfill that already ran stays (minus anyone who now qualifies outright); otherwise it has not run.
            return BackfillAsOf is null
                ? qualifying
                : qualifying with
                {
                    BackfillAsOf = BackfillAsOf,
                    BackfillEntries = [.. StoredDrafts(null).Where(d => !inField.Contains(d.BowlerId))],
                };
        }

        var appliedClearIds = qualifying.OverrideEffects.Where(e => e.Applied).Select(e => e.OverrideId).ToHashSet();
        var barred = earlier.SelectMany(s => s.Overrides)
            .Where(o => o.Kind == OverrideKind.Clear && o.Applied)
            .Concat(stage.Overrides.Where(o => o.Kind == OverrideKind.Clear && appliedClearIds.Contains(o.Id)))
            .Select(o => o.BowlerId)
            .ToHashSet();

        return qualifying with
        {
            BackfillAsOf = asOf,
            BackfillEntries = BackfillComputation.Compute(vacancies, inField, barred, facts.MembersOn(asOf), backfill.Points),
        };
    }

    private int VacanciesOf(SeriesStage stage)
        => (stage.SlotCount ?? 0) - _fieldEntries.Count(e => e.SourceStageId == stage.Id && !e.ViaOverride);

    private HashSet<BowlerId> BowlersQualifiedBefore(int order)
    {
        var earlierStageIds = _stages
            .Where(s => s.Kind == StageKind.Qualifying && s.Order < order)
            .Select(s => s.Id).ToHashSet();

        return [.. _fieldEntries
            .Where(e => e.SourceStageId is { } id && earlierStageIds.Contains(id))
            .Select(e => e.BowlerId)];
    }

    // Bowlers alive entering a non-Qualifying Stage: the previous Survivor Stage's advancers, else the whole Series Field.
    private List<BowlerId> FieldEnteringStage(SeriesStage stage)
    {
        var previousSurvivor = Ordered().LastOrDefault(s => s.Kind == StageKind.Survivor && s.Order < stage.Order);

        return previousSurvivor is null
            ? [.. _fieldEntries.Select(e => e.BowlerId)]
            : [.. previousSurvivor.SurvivorOutcomes.Where(o => o.Advanced).Select(o => o.BowlerId)];
    }

    // ---------- compare and write ----------

    private IEnumerable<FieldEntryDraft> StoredDrafts(SeriesStageId? source)
        => _fieldEntries.Where(e => e.SourceStageId == source).Select(e => e.ToDraft());

    private static bool SameSet<T>(IEnumerable<T> stored, IEnumerable<T> computed, Func<T, BowlerId> key)
        => stored.OrderBy(key).SequenceEqual(computed.OrderBy(key));

    private bool SameOutput(SeriesStage stage, StageComputation computed)
    {
        if (stage.Kind == StageKind.Survivor)
        {
            return SameSet(stage.SurvivorOutcomes.Select(o => o.ToDraft()), computed.Outcomes, d => d.BowlerId);
        }

        if (stage.Kind == StageKind.Placement)
        {
            return SameSet(_positions.Select(p => p.ToDraft()), computed.Positions, d => d.BowlerId);
        }

        var sameStage = stage.SlotCount == computed.SlotCount
            && SameSet(StoredDrafts(stage.Id), computed.Entries, d => d.BowlerId)
            && stage.Overrides.OrderBy(o => o.Id).Select(o => new OverrideEffect(o.Id, o.Applied))
                .SequenceEqual(computed.OverrideEffects.OrderBy(e => e.OverrideId));

        return sameStage
            && (LastQualifyingStage?.Id != stage.Id
                || (BackfillAsOf == computed.BackfillAsOf
                    && SameSet(StoredDrafts(null), computed.BackfillEntries, d => d.BowlerId)));
    }

    private void ApplyComputation(SeriesStage stage, StageComputation computed)
    {
        if (stage.Kind == StageKind.Survivor)
        {
            stage.ReplaceOutcomes(computed.Outcomes);
        }
        else if (stage.Kind == StageKind.Placement)
        {
            SyncPositions(computed.Positions);
        }
        else
        {
            stage.SetSlotCount(computed.SlotCount);
            SyncEntries(stage.Id, computed.Entries);

            foreach (var effect in computed.OverrideEffects)
            {
                stage.SetOverrideApplied(effect.OverrideId, effect.Applied);
            }

            if (LastQualifyingStage?.Id == stage.Id)
            {
                BackfillAsOf = computed.BackfillAsOf;
                SyncEntries(null, computed.BackfillEntries);
            }
        }
    }

    // Updates in place within the source (same bowler, same row), removes the rest, adds new ones. Never delete and re-add one
    // bowler inside a source, which the unique (series, bowler) index would punish.
    private void SyncEntries(SeriesStageId? source, IReadOnlyList<FieldEntryDraft> target)
    {
        var scope = _fieldEntries.Where(e => e.SourceStageId == source).ToList();

        foreach (var gone in scope.Where(e => target.All(d => d.BowlerId != e.BowlerId)))
        {
            _fieldEntries.Remove(gone);
        }

        foreach (var draft in target)
        {
            var existing = scope.Find(e => e.BowlerId == draft.BowlerId);
            if (existing is null)
            {
                _fieldEntries.Add(SeriesFieldEntry.Create(
                    draft.BowlerId, draft.SourceStageId, draft.Place, draft.BackfillPoints, draft.ViaOverride).Value);
            }
            else
            {
                _ = existing.Update(draft.Place, draft.BackfillPoints, draft.ViaOverride).Value;
            }
        }
    }

    // Composite key (series, bowler): update in place, so a bowler whose position changes is one UPDATE, not a delete and re-add.
    private void SyncPositions(IReadOnlyList<PositionDraft> target)
    {
        _positions.RemoveAll(p => target.All(d => d.BowlerId != p.BowlerId));

        foreach (var draft in target)
        {
            var existing = _positions.Find(p => p.BowlerId == draft.BowlerId);
            if (existing is null)
            {
                _positions.Add(SeriesPosition.Create(draft.BowlerId, draft.Position, draft.Bowled, draft.IsFinalist).Value);
            }
            else
            {
                _ = existing.Update(draft.Position, draft.Bowled, draft.IsFinalist).Value;
            }
        }
    }
}