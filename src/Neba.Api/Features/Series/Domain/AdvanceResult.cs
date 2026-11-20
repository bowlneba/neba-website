namespace Neba.Api.Features.Series.Domain;

/// <summary>What an advancement call did.</summary>
public sealed record AdvanceResult
{
    /// <summary>Nothing changed.</summary>
    public static AdvanceResult None { get; } = new();

    /// <summary>True when anything was written. <see cref="Series.Version"/> moved.</summary>
    public bool Changed { get; init; }

    /// <summary>True when a Stage's status changed. The handler evicts the tournaments cache.</summary>
    public bool StatusChanged { get; init; }

    /// <summary>Stages computed or recomputed with new output.</summary>
    public IReadOnlyList<SeriesStageId> ComputedStageIds { get; init; } = [];

    /// <summary>Stages marked stale.</summary>
    public IReadOnlyList<SeriesStageId> StaleStageIds { get; init; } = [];

    /// <summary>Stages made official.</summary>
    public IReadOnlyList<SeriesStageId> OfficialStageIds { get; init; } = [];

    /// <summary>The override an Add or Clear created.</summary>
    public StageOverrideId? OverrideId { get; init; }

    /// <summary>The Series' version after the call. Zero for <see cref="None"/>.</summary>
    public int Version { get; init; }
}