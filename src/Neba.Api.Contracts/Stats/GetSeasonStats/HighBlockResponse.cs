namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>
/// A single bowler's entry in the high block leaderboard for the Season.
/// A block is the highest score achieved across a 5-game qualifying window.
/// The collection is ordered by high block descending; rank should be derived by the consumer.
/// </summary>
public sealed record HighBlockResponse
{
    /// <summary>The unique identifier of the Bowler (ULID string).</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>The highest 5-game qualifying block score the bowler achieved across all tournaments in the Season.</summary>
    public required int HighBlock { get; init; }

    /// <summary>The highest single qualifying game the bowler bowled across all tournaments in the Season.</summary>
    public required int HighGame { get; init; }
}
