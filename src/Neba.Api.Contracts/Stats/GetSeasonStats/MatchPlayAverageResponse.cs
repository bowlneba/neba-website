namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>
/// A single bowler's entry in the match play average leaderboard for the Season.
/// Only includes bowlers who reached Finals. The collection is ordered by match play average descending;
/// rank should be derived by the consumer.
/// </summary>
public sealed record MatchPlayAverageResponse
{
    /// <summary>The unique identifier of the Bowler (ULID string).</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>The bowler's match play average (match play pinfall divided by match play games) for the Season.</summary>
    public required decimal MatchPlayAverage { get; init; }

    /// <summary>Total individual match play games bowled across all Finals appearances in the Season.</summary>
    public required int Games { get; init; }

    /// <summary>Total head-to-head match play victories recorded across all Finals appearances in the Season.</summary>
    public required int Wins { get; init; }

    /// <summary>Total head-to-head match play defeats recorded across all Finals appearances in the Season.</summary>
    public required int Losses { get; init; }

    /// <summary>Win percentage in match play for the Season (wins divided by total matches, expressed as a percentage).</summary>
    public required decimal WinPercentage { get; init; }

    /// <summary>Total tournament cash prize money earned by the bowler during the Season, excluding Cup earnings.</summary>
    public required decimal Winnings { get; init; }
}
