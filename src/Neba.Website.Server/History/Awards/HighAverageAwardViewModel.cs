namespace Neba.Website.Server.History.Awards;

/// <summary>
/// View model used by the High Average UI to display a single season's award entry.
/// </summary>
public sealed record HighAverageAwardViewModel
{
    /// <summary>
    /// The season in which the high average was achieved (e.g., "2025 Season").
    /// </summary>
    public required string Season { get; init; }

    /// <summary>
    /// The full display name of the bowler who received the award.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// The bowler's average pinfall per game across all stat-eligible tournaments during the season.
    /// </summary>
    public required decimal Average { get; init; }

    /// <summary>
    /// The total number of games bowled in stat-eligible tournaments during the season.
    /// <c>null</c> for historical seasons where game counts were not tracked.
    /// </summary>
    public int? TotalGames { get; init; }

    /// <summary>
    /// The number of stat-eligible tournaments the bowler participated in during the season.
    /// <c>null</c> for historical seasons where tournament counts were not tracked.
    /// </summary>
    public int? TournamentsParticipated { get; init; }
}