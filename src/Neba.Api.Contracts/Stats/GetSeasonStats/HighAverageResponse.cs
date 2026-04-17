namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>
/// A single bowler's entry in the high average leaderboard for the Season.
/// The collection is ordered by average descending; rank should be derived by the consumer.
/// </summary>
public sealed record HighAverageResponse
{
    /// <summary>The unique identifier of the Bowler (ULID string).</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>The bowler's overall average (total pinfall divided by total games) for the Season.</summary>
    public required decimal Average { get; init; }

    /// <summary>Total games bowled by the bowler across all tournaments in the Season.</summary>
    public required int Games { get; init; }

    /// <summary>Total tournaments the bowler participated in during the Season.</summary>
    public required int Tournaments { get; init; }

    /// <summary>
    /// The bowler's field average for the Season — average performance relative to the competitive field,
    /// expressed as a signed decimal where a positive value indicates above-field performance.
    /// </summary>
    public required decimal FieldAverage { get; init; }
}
