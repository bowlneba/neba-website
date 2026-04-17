namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>
/// Complete statistics for a single bowler in the Season, used to populate the full-stat modal.
/// The collection is ordered by Bowler of the Year points descending; rank should be derived by the consumer.
/// </summary>
public sealed record FullStatModalRowResponse
{
    /// <summary>The unique identifier of the Bowler (ULID string).</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>Total Bowler of the Year (Open) points accumulated during the Season.</summary>
    public required int Points { get; init; }

    /// <summary>The bowler's overall average (total pinfall divided by total games) for the Season.</summary>
    public required decimal Average { get; init; }

    /// <summary>Total games bowled across all tournaments in the Season.</summary>
    public required int Games { get; init; }

    /// <summary>Number of tournaments in which the bowler advanced to the Finals (match play round).</summary>
    public required int Finals { get; init; }

    /// <summary>Total head-to-head match play victories recorded across all Finals appearances in the Season.</summary>
    public required int Wins { get; init; }

    /// <summary>Total head-to-head match play defeats recorded across all Finals appearances in the Season.</summary>
    public required int Losses { get; init; }

    /// <summary>Win percentage in match play for the Season (wins divided by total matches, expressed as a percentage).</summary>
    public required decimal WinPercentage { get; init; }

    /// <summary>The bowler's match play average (match play pinfall divided by match play games) for the Season. Zero if the bowler did not reach Finals.</summary>
    public required decimal MatchPlayAverage { get; init; }

    /// <summary>Total tournament cash prize money earned by the bowler during the Season, excluding Cup earnings.</summary>
    public required decimal Winnings { get; init; }

    /// <summary>
    /// The bowler's field average for the Season — performance relative to the competitive field,
    /// expressed as a signed decimal where a positive value indicates above-field performance.
    /// </summary>
    public required decimal FieldAverage { get; init; }

    /// <summary>Total tournaments the bowler participated in during the Season.</summary>
    public required int Tournaments { get; init; }
}