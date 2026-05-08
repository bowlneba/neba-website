namespace Neba.Api.Contracts.Tournaments.GetTournament;

/// <summary>
/// Result for a single bowler in a tournament.
/// </summary>
public sealed record TournamentResultResponse
{
    /// <summary>
    /// Display name of the bowler.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// Finishing place; null when place was not recorded.
    /// </summary>
    public int? Place { get; init; }

    /// <summary>
    /// Prize money awarded in USD.
    /// </summary>
    public decimal PrizeMoney { get; init; }

    /// <summary>
    /// Season points awarded.
    /// </summary>
    public int Points { get; init; }

    /// <summary>
    /// Name of the side cut the bowler competed in; null for the main cut.
    /// </summary>
    public string? SideCutName { get; init; }

    /// <summary>
    /// Display color for the side cut as a CSS hex string (e.g., "#FF5733"); null for the main cut.
    /// </summary>
    public string? SideCutIndicator { get; init; }
}