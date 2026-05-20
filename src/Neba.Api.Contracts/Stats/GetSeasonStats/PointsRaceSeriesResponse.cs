namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>
/// A single bowler's Bowler of the Year points race series for the Season, showing cumulative points earned after each tournament.
/// </summary>
public sealed record PointsRaceSeriesResponse
{
    /// <summary>
    /// The unique identifier of the Bowler (ULID string).
    /// </summary>
    public required string BowlerId { get; init; }

    /// <summary>
    /// The bowler's display name.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// The ordered sequence of tournament results, each showing the cumulative Bowler of the Year points after that tournament.
    /// </summary>
    public required IReadOnlyCollection<PointsRaceTournamentResponse> Results { get; init; }
}