namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>
/// A single bowler's entry in the Finals-per-eligible-entry efficiency leaderboard for the Season.
/// Only includes bowlers with at least one eligible entry and at least one Finals appearance.
/// The collection is ordered by finals per entry descending; rank should be derived by the consumer.
/// </summary>
public sealed record FinalsPerEntryResponse
{
    /// <summary>The unique identifier of the Bowler (ULID string).</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>Number of tournaments in which the bowler advanced to the Finals (match play round).</summary>
    public required int Finals { get; init; }

    /// <summary>Number of eligible entries the bowler made during the Season.</summary>
    public required int Entries { get; init; }

    /// <summary>Finals appearances divided by eligible entries, rounded to two decimal places.</summary>
    public required decimal FinalsPerEntry { get; init; }
}
