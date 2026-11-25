namespace Neba.Api.Contracts.Series.GetSeriesAdvancement;

/// <summary>One tournament's advancement status and output.</summary>
public sealed record StageAdvancementResponse
{
    /// <summary>The Series tournament id.</summary>
    public required string StageId { get; init; }

    /// <summary>The order.</summary>
    public required int Order { get; init; }

    /// <summary>The kind name: Qualifying, Survivor or Placement.</summary>
    public required string Kind { get; init; }

    /// <summary>The status name: Pending, Computed or Official.</summary>
    public required string Status { get; init; }

    /// <summary>True when the stored output may differ from a recompute.</summary>
    public required bool IsStale { get; init; }

    /// <summary>True for a Survivor tournament with no squad, or whose squad no longer exists.</summary>
    public required bool NeedsSquad { get; init; }

    /// <summary>When the tournament last computed.</summary>
    public DateTimeOffset? ComputedAt { get; init; }

    /// <summary>True when the tournament is Computed, not stale, and every earlier tournament is Official (and the Bowler of the Year point list ran, for the last Qualifying tournament). A person can mark it Official.</summary>
    public required bool CanMakeOfficial { get; init; }

    /// <summary>When the tournament became Official.</summary>
    public DateTimeOffset? OfficialAt { get; init; }

    /// <summary>The Qualifying slot count.</summary>
    public int? SlotCount { get; init; }

    /// <summary>The Qualifying slots that went vacant.</summary>
    public int? VacantSlotCount { get; init; }

    /// <summary>The tournament id.</summary>
    public required string TournamentId { get; init; }

    /// <summary>The tournament status name. Cancelled means the tournament can be removed.</summary>
    public required string TournamentStatus { get; init; }

    /// <summary>The squad the Survivor tournament uses, if resolved.</summary>
    public string? ResolvedSquadId { get; init; }

    /// <summary>Field entries. The last Qualifying tournament's list includes the Bowler of the Year point list (null SourceStageId).</summary>
    public required IReadOnlyList<FieldEntryResponse> Entries { get; init; }

    /// <summary>Bowlers inside the cut who already advanced from an earlier tournament. Their slots are vacant.</summary>
    public required IReadOnlyList<AlreadyAdvancedResponse> AlreadyAdvanced { get; init; }

    /// <summary>The tournament's overrides.</summary>
    public required IReadOnlyList<StageOverrideResponse> Overrides { get; init; }

    /// <summary>The Survivor outcomes. Empty for other kinds.</summary>
    public required IReadOnlyList<SurvivorOutcomeResponse> SurvivorOutcomes { get; init; }
}