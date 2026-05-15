namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>
/// High-level participation and financial summary for the Season.
/// </summary>
public sealed record SeasonAtAGlanceResponse
{
    /// <summary>
    /// Total number of tournament entries made across all tournaments in the Season.
    /// </summary>
    public required int TotalEntries { get; init; }

    /// <summary>
    /// Total cash prize money awarded across all tournaments in the Season.
    /// </summary>
    public required decimal TotalPrizeMoney { get; init; }
}