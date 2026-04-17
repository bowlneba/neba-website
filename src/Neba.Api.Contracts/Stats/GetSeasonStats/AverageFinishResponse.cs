namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>
/// A single bowler's entry in the average finish leaderboard for the Season.
/// Only includes bowlers who received at least one finishing position. The collection is ordered by
/// average finish ascending (lower is better); rank should be derived by the consumer.
/// </summary>
public sealed record AverageFinishResponse
{
    /// <summary>The unique identifier of the Bowler (ULID string).</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>The bowler's mean finishing position across all Finals appearances in which they received a finish.</summary>
    public required decimal AverageFinish { get; init; }

    /// <summary>Number of tournaments in which the bowler advanced to the Finals (match play round).</summary>
    public required int Finals { get; init; }

    /// <summary>Total tournament cash prize money earned by the bowler during the Season, excluding Cup earnings.</summary>
    public required decimal Winnings { get; init; }
}