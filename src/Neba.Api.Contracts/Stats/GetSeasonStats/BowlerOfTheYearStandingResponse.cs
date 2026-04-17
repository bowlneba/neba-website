namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>
/// A single bowler's standing in an award category (Bowler of the Year, Senior of the Year,
/// Super Senior of the Year, Woman of the Year, Rookie of the Year, or Youth of the Year).
/// The collection is ordered by points descending; rank is not included and should be derived by the consumer.
/// </summary>
public sealed record BowlerOfTheYearStandingResponse
{
    /// <summary>The unique identifier of the Bowler (ULID string).</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>Total points accumulated toward this award category for the Season.</summary>
    public required int Points { get; init; }

    /// <summary>Number of eligible tournaments the bowler participated in during the Season.</summary>
    public required int Tournaments { get; init; }

    /// <summary>Number of eligible entries the bowler made during the Season.</summary>
    public required int Entries { get; init; }

    /// <summary>Number of tournaments in which the bowler advanced to the Finals (match play round).</summary>
    public required int Finals { get; init; }

    /// <summary>The bowler's mean finishing position across all Finals appearances. Null if the bowler did not receive a finishing position.</summary>
    public decimal? AverageFinish { get; init; }

    /// <summary>Total tournament cash prize money earned by the bowler during the Season, excluding Cup earnings.</summary>
    public required decimal Winnings { get; init; }
}
