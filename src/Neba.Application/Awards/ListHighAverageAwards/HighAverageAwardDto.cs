using Neba.Domain.Bowlers;

namespace Neba.Application.Awards.ListHighAverageAwards;

/// <summary>
/// Data Transfer Object representing the High Average Award, which recognizes the highest pinfall average per game across all Stat-Eligible Tournaments in a Season. Eligibility requires a minimum of floor(4.5 × Stat-Eligible Tournaments completed) games bowled. Baker team finals games are excluded from average and game total calculations.
/// </summary>
public sealed record HighAverageAwardDto
{
    /// <summary>
    /// The season in which the award was earned.
    /// </summary>
    public required string Season { get; init; }

    /// <summary>
    /// The name of the bowler receiving the award.
    /// </summary>
    public required Name BowlerName { get; init; }

    /// <summary>
    /// The winner's pinfall average.
    /// </summary>
    public required decimal Average { get; init; }

    /// <summary>
    /// The total number of games bowled by the winner.
    /// </summary>
    public int? TotalGames { get; init; }

    /// <summary>
    /// The number of Stat-Eligible Tournaments the winner participated in.
    /// </summary>
    public int? TournamentsParticipated { get; init; }
}