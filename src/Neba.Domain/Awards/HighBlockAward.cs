using Neba.Domain.Bowlers;

namespace Neba.Domain.Awards;

/// <summary>
/// Recognizes the single highest 5-game pinfall total from a qualifying block
/// (before match play) in any Stat-Eligible Tournament during the Season.
/// </summary>
public sealed class HighBlockAward
{
    /// <summary>
    /// System-generated unique identifier.
    /// </summary>
    public required SeasonAwardId Id { get; init; }

    /// <summary>
    /// The bowler receiving the award.
    /// </summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>
    /// The winning 5-game pinfall total.
    /// </summary>
    public required int BlockScore { get; init; }

    /// <summary>
    /// Navigation to the bowler. Internal — for EF Core query projections only.
    /// </summary>
    internal Bowler Bowler { get; init; } = null!;
}
