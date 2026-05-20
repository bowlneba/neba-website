using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Stats.GetSeasonStats;

/// <summary>
/// A leaderboard entry for the High Average leaderboard, representing a bowler's overall pin average
/// across all games in the season. Ordered descending by average. Bowlers with zero games are excluded.
/// </summary>
public sealed record HighAverageDto
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
    /// Pre-computed overall pin average across all games in the season.
    /// </summary>
    public required decimal Average { get; init; }

    /// <summary>
    /// Total games bowled during the season.
    /// </summary>
    public required int Games { get; init; }

    /// <summary>
    /// The number of distinct tournaments the bowler participated in during the season.
    /// </summary>
    public required int Tournaments { get; init; }

    /// <summary>
    /// The bowler's average performance relative to the competitive field across tournaments in the season.
    /// </summary>
    public required decimal FieldAverage { get; init; }
}