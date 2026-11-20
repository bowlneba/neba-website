namespace Neba.Api.Contracts.Series.GetSeriesAdvancement;

/// <summary>One Stage's advancement status and output.</summary>
public sealed record StageAdvancementResponse
{
    /// <summary>The Stage id.</summary>
    public required string StageId { get; init; }

    /// <summary>The order.</summary>
    public required int Order { get; init; }

    /// <summary>The kind name: Qualifying, Survivor or Placement.</summary>
    public required string Kind { get; init; }

    /// <summary>The status name: Pending, Computed or Official.</summary>
    public required string Status { get; init; }

    /// <summary>True when the stored output may differ from a recompute.</summary>
    public required bool IsStale { get; init; }

    /// <summary>True for a Survivor Stage with no squad, or whose squad no longer exists.</summary>
    public required bool NeedsSquad { get; init; }

    /// <summary>When the Stage last computed.</summary>
    public DateTimeOffset? ComputedAt { get; init; }

    /// <summary>When a Computed Stage will become Official (up to 15 minutes later). Null when it will not.</summary>
    public DateTimeOffset? OfficialDueAt { get; init; }

    /// <summary>When the Stage became Official.</summary>
    public DateTimeOffset? OfficialAt { get; init; }

    /// <summary>The Qualifying slot count.</summary>
    public int? SlotCount { get; init; }

    /// <summary>The Qualifying slots that went vacant.</summary>
    public int? VacantSlotCount { get; init; }

    /// <summary>The tournament id.</summary>
    public required string TournamentId { get; init; }

    /// <summary>The tournament status name. Cancelled means the Stage can be removed.</summary>
    public required string TournamentStatus { get; init; }

    /// <summary>The squad the Survivor Stage uses, if resolved.</summary>
    public string? ResolvedSquadId { get; init; }

    /// <summary>Field entries. The last Qualifying Stage's list includes the Backfill (null SourceStageId).</summary>
    public required IReadOnlyList<FieldEntryResponse> Entries { get; init; }

    /// <summary>The Stage's overrides.</summary>
    public required IReadOnlyList<StageOverrideResponse> Overrides { get; init; }

    /// <summary>The Survivor outcomes. Empty for other kinds.</summary>
    public required IReadOnlyList<SurvivorOutcomeResponse> SurvivorOutcomes { get; init; }
}