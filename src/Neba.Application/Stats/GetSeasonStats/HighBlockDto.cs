using Neba.Domain.Bowlers;

namespace Neba.Application.Stats.GetSeasonStats;

/// <summary>
/// A leaderboard entry for the High Block leaderboard, representing a bowler's highest 5-game block
/// score in the season. Ordered descending by block score. Bowlers with a zero block score are excluded.
/// </summary>
public sealed record HighBlockDto
{
    /// <summary>The unique identifier of the bowler.</summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required Name BowlerName { get; init; }

    /// <summary>The highest 5-game qualifying block score achieved during the season.</summary>
    public required int HighBlock { get; init; }

    /// <summary>The highest single qualifying game achieved during the season.</summary>
    public required int HighGame { get; init; }
}
