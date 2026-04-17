namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>
/// A single bowler's entry in the points-per-eligible-entry efficiency leaderboard for the Season.
/// Only includes bowlers with at least one eligible entry and at least one point.
/// The collection is ordered by points per entry descending; rank should be derived by the consumer.
/// </summary>
public sealed record PointsPerEntryResponse
{
    /// <summary>The unique identifier of the Bowler (ULID string).</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>Bowler of the Year points divided by eligible entries, rounded to two decimal places.</summary>
    public required decimal PointsPerEntry { get; init; }

    /// <summary>Total Bowler of the Year points accumulated during the Season.</summary>
    public required int Points { get; init; }

    /// <summary>Number of eligible entries the bowler made during the Season.</summary>
    public required int Entries { get; init; }
}
