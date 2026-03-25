namespace Neba.Api.Contracts.Awards;

/// <summary>
/// Data Transfer Object representing the response for a High Average award for a season.
/// </summary>
public sealed record HighAverageAwardResponse
{
    /// <summary>
    /// Gets the season in which the award was earned.
    /// </summary>
    public required string Season { get; init; }

    /// <summary>
    /// Gets the name of the bowler who received the award.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// Gets the winner's pinfall average.
    /// </summary>
    public required decimal Average { get; init; }

    /// <summary>
    /// Gets the total number of games bowled by the winner.
    /// </summary>
    public int? TotalGames { get; init; }

    /// <summary>
    /// Gets the number of Stat-Eligible Tournaments the winner participated in.
    /// </summary>
    public int? TournamentsParticipated { get; init; }
}
