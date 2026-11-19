namespace Neba.Api.Contracts.Series.GetSeries;

/// <summary>Series finale response.</summary>
public sealed record SeriesFinaleResponse
{
    /// <summary>The finalist count.</summary>
    public required int FinalistCount { get; init; }
    /// <summary>The date.</summary>
    public required DateOnly Date { get; init; }
    /// <summary>The start time.</summary>
    public required TimeOnly StartTime { get; init; }

    /// <summary>Host center, or null while it is to be announced.</summary>
    public string? BowlingCenterId { get; init; }
    /// <summary>The bowling center name.</summary>
    public string? BowlingCenterName { get; init; }
    /// <summary>The bowling center city.</summary>
    public string? BowlingCenterCity { get; init; }
    /// <summary>The bowling center state.</summary>
    public string? BowlingCenterState { get; init; }
}