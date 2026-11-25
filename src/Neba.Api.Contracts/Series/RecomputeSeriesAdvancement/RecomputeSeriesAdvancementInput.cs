namespace Neba.Api.Contracts.Series.RecomputeSeriesAdvancement;

/// <summary>Recompute series advancement input.</summary>
public sealed record RecomputeSeriesAdvancementInput
{
    /// <summary>The Version from the last GET.</summary>
    public required int Version { get; init; }

    /// <summary>True to reset official tournaments in the range to Computed.</summary>
    public bool ConfirmResetOfficial { get; init; }
}