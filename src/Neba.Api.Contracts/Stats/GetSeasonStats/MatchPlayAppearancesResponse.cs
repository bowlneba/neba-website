namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>
/// A single bowler's entry in the match play appearances leaderboard for the Season.
/// Only includes bowlers who reached Finals at least once. The collection is ordered by Finals descending;
/// rank should be derived by the consumer.
/// </summary>
public sealed record MatchPlayAppearancesResponse
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
    /// Number of tournaments in which the bowler advanced to the Finals (match play round).
    /// </summary>
    public required int Finals { get; init; }

    /// <summary>
    /// Stat-Eligible Tournaments the bowler participated in during the Season.
    /// </summary>
    public required int Tournaments { get; init; }

    /// <summary>
    /// Entries the bowler made in Stat-Eligible Tournaments during the Season.
    /// </summary>
    public required int Entries { get; init; }
}