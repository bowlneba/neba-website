namespace Neba.Website.Server.Stats;

/// <summary>
/// Represents a summary of the season's statistics.
/// </summary>
public sealed record SeasonAtAGlanceViewModel
{
    /// <summary>
    /// The total number of entries for the season.
    /// </summary>
    public required int TotalEntries { get; init; }

    /// <summary>
    /// The total prize money for the season.
    /// </summary>
    public required decimal TotalPrizeMoney { get; init; }
}