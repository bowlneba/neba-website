namespace Neba.Website.Server.Series;

/// <summary>What the signed-in user may do in the advancement section.</summary>
/// <param name="CanView">Holds Series.EditSeries.</param>
/// <param name="CanManage">Holds Series.EditSeries and Series.ManageAdvancement.</param>
internal readonly record struct AdvancementAccess(bool CanView, bool CanManage);

/// <summary>The advancement read, shaped for the page.</summary>
internal sealed record SeriesAdvancementViewModel
{
    public required int Version { get; init; }
    public DateOnly? BackfillAsOf { get; init; }
    public required int TargetField { get; init; }
    public required IReadOnlyList<StageAdvancementViewModel> Stages { get; init; }
    public required IReadOnlyList<PositionViewModel> Positions { get; init; }

    /// <summary>True when any position has a Finalist flag, so the Series has a Finale.</summary>
    public bool HasFinalists => Positions.Any(p => p.IsFinalist is not null);

    public StageAdvancementViewModel? StageById(string stageId)
        => Stages.FirstOrDefault(s => s.Id == stageId);

    /// <summary>The Official Stages a recompute from <paramref name="order"/> would reset.</summary>
    public IReadOnlyList<int> OfficialOrdersFrom(int order)
        => [.. Stages.Where(s => s.Order >= order && s.Status == StageStatuses.Official).Select(s => s.Order)];

    /// <summary>Result rows an Add override can take: not in the Series field, and no override on that Stage.</summary>
    public IReadOnlyList<StageResultViewModel> AddCandidates(string stageId, IReadOnlyList<StageResultViewModel> results)
    {
        var taken = Stages.SelectMany(s => s.Entries).Select(e => e.BowlerId).ToHashSet(StringComparer.Ordinal);
        if (StageById(stageId) is { } stage)
        {
            taken.UnionWith(stage.Overrides.Select(o => o.BowlerId));
        }

        return [.. results.Where(r => !taken.Contains(r.BowlerId))];
    }
}

/// <summary>One Stage on the season rail.</summary>
/// <param name="Id">The Stage ID.</param>
/// <param name="Order">Position in the season, from 1.</param>
/// <param name="Kind">Qualifying, Survivor or Placement.</param>
/// <param name="Status">Pending, Computed or Official.</param>
/// <param name="IsStale">The stored output may differ from a recompute.</param>
/// <param name="NeedsSquad">A Survivor Stage with no squad, or whose squad vanished.</param>
/// <param name="OfficialDueAt">When a Computed Stage becomes Official, in the viewer's time zone.</param>
/// <param name="SlotCount">Qualifying slots, once computed.</param>
/// <param name="VacantSlotCount">Slots not filled by a qualifier.</param>
/// <param name="TournamentId">The Stage's tournament.</param>
/// <param name="TournamentStatus">The tournament status name.</param>
/// <param name="ResolvedSquadId">The squad a Survivor Stage uses, when known.</param>
/// <param name="IsLastQualifying">True for the last Qualifying Stage, which owns the Backfill.</param>
/// <param name="Entries">Field entries for this Stage (the last Qualifying Stage also holds the Backfill).</param>
/// <param name="AlreadyAdvanced">Bowlers inside the cut who already advanced from an earlier Stage.</param>
/// <param name="Overrides">Admin overrides on this Stage.</param>
/// <param name="Outcomes">Survivor results.</param>
/// <param name="CanAddOverride">Admin may add a bowler.</param>
/// <param name="CanRemoveOverride">Admin may remove an override.</param>
/// <param name="CanRecompute">Holder may recompute from this Stage.</param>
/// <param name="CanPickSquad">Holder may pick this Survivor Stage's squad.</param>
public sealed record StageAdvancementViewModel(
    string Id,
    int Order,
    string Kind,
    string Status,
    bool IsStale,
    bool NeedsSquad,
    DateTimeOffset? OfficialDueAt,
    int? SlotCount,
    int? VacantSlotCount,
    string TournamentId,
    string TournamentStatus,
    string? ResolvedSquadId,
    bool IsLastQualifying,
    IReadOnlyList<FieldEntryViewModel> Entries,
    IReadOnlyList<AlreadyAdvancedViewModel> AlreadyAdvanced,
    IReadOnlyList<StageOverrideViewModel> Overrides,
    IReadOnlyList<SurvivorOutcomeViewModel> Outcomes,
    bool CanAddOverride,
    bool CanRemoveOverride,
    bool CanRecompute,
    bool CanPickSquad)
{
    /// <summary>The tournament was cancelled, so the Stage can be removed.</summary>
    public bool IsTournamentCancelled => TournamentStatus == TournamentStatuses.Cancelled;
}

/// <summary>A bowler's place in the Series field.</summary>
/// <param name="BowlerId">The bowler.</param>
/// <param name="BowlerName">The bowler's name.</param>
/// <param name="SourceStageId">The Qualifying Stage that produced it; null for the Backfill.</param>
/// <param name="Place">Place in that Stage's tournament; null for the Backfill.</param>
/// <param name="BackfillPoints">Points that earned a Backfill entry.</param>
/// <param name="ViaOverride">An admin added the bowler.</param>
/// <param name="CanClear">Admin may clear this slot.</param>
public sealed record FieldEntryViewModel(
    string BowlerId, string BowlerName, string? SourceStageId, int? Place, int? BackfillPoints, bool ViaOverride, bool CanClear)
{
    /// <summary>The entry came from the Backfill.</summary>
    public bool IsBackfill => SourceStageId is null;
}

/// <summary>A bowler inside a Stage's cut who already advanced from an earlier Stage.</summary>
/// <param name="BowlerId">The bowler.</param>
/// <param name="BowlerName">The bowler's name.</param>
/// <param name="Place">Place in this Stage's tournament.</param>
/// <param name="EarlierStageOrder">The earlier Stage the bowler advanced from.</param>
public sealed record AlreadyAdvancedViewModel(string BowlerId, string BowlerName, int Place, int EarlierStageOrder);

/// <summary>An admin Add or Clear on a Qualifying Stage.</summary>
/// <param name="OverrideId">The override.</param>
/// <param name="BowlerId">The bowler added or cleared.</param>
/// <param name="BowlerName">The bowler's name.</param>
/// <param name="Kind">Add or Clear.</param>
/// <param name="Reason">Why the admin made the change.</param>
/// <param name="CreatedAt">When, in the viewer's time zone.</param>
/// <param name="Applied">False once the override's target changed.</param>
public sealed record StageOverrideViewModel(
    string OverrideId, string BowlerId, string BowlerName, string Kind, string Reason, DateTimeOffset CreatedAt, bool Applied);

/// <summary>A bowler's Survivor Round result.</summary>
/// <param name="BowlerId">The bowler.</param>
/// <param name="BowlerName">The bowler's name.</param>
/// <param name="BlockTotal">Null when the bowler has no full block.</param>
/// <param name="Advanced">The bowler advanced.</param>
public sealed record SurvivorOutcomeViewModel(string BowlerId, string BowlerName, int? BlockTotal, bool Advanced);

/// <summary>A bowler's Placement Round position.</summary>
/// <param name="BowlerId">The bowler.</param>
/// <param name="BowlerName">The bowler's name.</param>
/// <param name="Position">1-based position.</param>
/// <param name="Bowled">The bowler bowled the Placement Stage.</param>
/// <param name="IsFinalist">Null when the Series has no Finale.</param>
/// <param name="TournamentPlace">Place in the Placement Stage's tournament; null when unplaced.</param>
public sealed record PositionViewModel(string BowlerId, string BowlerName, int Position, bool Bowled, bool? IsFinalist, int? TournamentPlace = null);

/// <summary>A result row the Add bowler picker offers.</summary>
/// <param name="BowlerId">The bowler.</param>
/// <param name="BowlerName">The bowler's name.</param>
/// <param name="Place">Place in the Stage's tournament.</param>
/// <param name="Label">Picker text, such as "Marcus Alvarez (12th)".</param>
public sealed record StageResultViewModel(string BowlerId, string BowlerName, int Place, string Label);

/// <summary>A squad the Pick squad dialog offers.</summary>
/// <param name="SquadId">The squad.</param>
/// <param name="Label">"9:30 AM on 7/12".</param>
/// <param name="Detail">"Has scores · Current".</param>
/// <param name="HasScores">The squad has score rows.</param>
/// <param name="IsSelected">The Stage uses this squad now.</param>
public sealed record StageSquadViewModel(string SquadId, string Label, string Detail, bool HasScores, bool IsSelected);

/// <summary>What the override dialog hands the page.</summary>
/// <param name="BowlerId">The bowler to add or clear.</param>
/// <param name="BowlerName">For the toast.</param>
/// <param name="Reason">Trimmed.</param>
public sealed record StageOverrideSubmission(string BowlerId, string BowlerName, string Reason);