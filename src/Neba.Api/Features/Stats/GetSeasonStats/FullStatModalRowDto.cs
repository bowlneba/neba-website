using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Stats.GetSeasonStats;

/// <summary>
/// A row in the Full Stat Modal containing all key statistics for a single bowler across the season.
/// Ordered descending by Bowler of the Year points. Derived fields (Average, WinPercentage,
/// MatchPlayAverage) are pre-computed.
/// </summary>
public sealed record FullStatModalRowDto
{
    /// <summary>
    /// The unique identifier of the bowler.
    /// </summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>
    /// The bowler's display name.
    /// </summary>
    public required Name BowlerName { get; init; }

    /// <summary>
    /// Bowler of the Year points accumulated during the season.
    /// </summary>
    public required int Points { get; init; }

    /// <summary>
    /// Pre-computed overall pin average across all games in the season.
    /// </summary>
    public required decimal Average { get; init; }

    /// <summary>
    /// Total games bowled across all tournaments in the season.
    /// </summary>
    public required int Games { get; init; }

    /// <summary>
    /// The number of Finals appearances during the season.
    /// </summary>
    public required int Finals { get; init; }

    /// <summary>
    /// Total match play victories during the season.
    /// </summary>
    public required int Wins { get; init; }

    /// <summary>
    /// Total match play defeats during the season.
    /// </summary>
    public required int Losses { get; init; }

    /// <summary>
    /// Pre-computed match play win percentage.
    /// </summary>
    public required decimal WinPercentage { get; init; }

    /// <summary>
    /// Pre-computed average pins per match play game during the season.
    /// </summary>
    public required decimal MatchPlayAverage { get; init; }

    /// <summary>
    /// Total prize money earned during the season.
    /// </summary>
    public required decimal Winnings { get; init; }

    /// <summary>
    /// The bowler's average performance relative to the competitive field across tournaments in the season.
    /// </summary>
    public required decimal FieldAverage { get; init; }

    /// <summary>
    /// The number of distinct tournaments the bowler participated in during the season.
    /// </summary>
    public required int Tournaments { get; init; }
}