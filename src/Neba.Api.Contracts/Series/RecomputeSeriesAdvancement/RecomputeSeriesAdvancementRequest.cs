namespace Neba.Api.Contracts.Series.RecomputeSeriesAdvancement;

/// <summary>Recompute series advancement request.</summary>
public sealed record RecomputeSeriesAdvancementRequest
{
    /// <summary>The series id.</summary>
    public required string Id { get; init; }

    /// <summary>The tournament to recompute from.</summary>
    public required string StageId { get; init; }

    /// <summary>The recomputation.</summary>
    public required RecomputeSeriesAdvancementInput Recomputation { get; init; }
}