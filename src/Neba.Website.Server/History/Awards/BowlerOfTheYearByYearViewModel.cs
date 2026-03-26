namespace Neba.Website.Server.History.Awards;

/// <summary>
/// View model representing all Bowler of the Year award winners for a single season, grouped by category.
/// </summary>
public sealed record BowlerOfTheYearByYearViewModel
{
    /// <summary>
    /// The season for which the Bowler of the Year awards apply (e.g., "2025 Season").
    /// </summary>
    public required string Season { get; init; }

    /// <summary>
    /// The award winners for each category in display order.
    /// Key is the UI display label (e.g., "Bowler of the Year", "Senior", "Woman").
    /// Value is the full display name of the winning bowler.
    /// </summary>
    public required IReadOnlyList<KeyValuePair<string, string>> WinnersByCategory { get; init; }
}