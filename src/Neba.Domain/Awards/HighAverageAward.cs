using Neba.Domain.Bowlers;

namespace Neba.Domain.Awards;

/// <summary>
/// Recognizes the highest pinfall average per game across all Stat-Eligible Tournaments in the Season.
/// Eligibility requires a minimum of floor(4.5 × Stat-Eligible Tournaments completed) games bowled.
/// Baker team finals games are excluded from average and game total calculations.
/// </summary>
public sealed class HighAverageAward
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
    /// The winner's pinfall average per game.
    /// </summary>
    public required decimal Average { get; init; }

    /// <summary>
    /// Total games bowled across Stat-Eligible Tournaments.
    /// </summary>
    public required int TotalGames { get; init; }

    /// <summary>
    /// Number of Stat-Eligible Tournaments the bowler participated in.
    /// </summary>
    public required int TournamentsParticipated { get; init; }

    /// <summary>
    /// Navigation to the bowler. Internal — for EF Core query projections only.
    /// </summary>
    internal Bowler Bowler { get; init; } = null!;
}
