namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>
/// A single bowler's entry in the points-per-eligible-tournament efficiency leaderboard for the Season.
/// Only includes bowlers with at least one eligible tournament and at least one point.
/// The collection is ordered by points per tournament descending; rank should be derived by the consumer.
/// </summary>
public sealed record PointsPerTournamentResponse
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
    /// Total Bowler of the Year points accumulated during the Season.
    /// </summary>
    public required int Points { get; init; }

    /// <summary>
    /// Number of eligible tournaments the bowler participated in during the Season.
    /// </summary>
    public required int Tournaments { get; init; }

    /// <summary>
    /// Bowler of the Year points divided by eligible tournaments, rounded to two decimal places.
    /// </summary>
    public required decimal PointsPerTournament { get; init; }
}