using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Seasons.Domain;

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
    public int? TotalGames { get; init; }

    /// <summary>
    /// Number of Stat-Eligible Tournaments the bowler participated in.
    /// </summary>
    public int? TournamentsParticipated { get; init; }

    /// <summary>
    /// Navigation to the bowler. Internal — for EF Core query projections only.
    /// </summary>
    internal Bowler Bowler { get; init; } = null!;

    internal static ErrorOr<HighAverageAward> Create(
        BowlerId bowlerId,
        decimal average,
        int totalGames,
        int tournamentsParticipated
    )
    {
        if (bowlerId.Equals(default))
        {
            return HighAverageAwardErrors.BowlerIdRequired;
        }

        if (average <= 0)
        {
            return HighAverageAwardErrors.InvalidAverage;
        }

        if (totalGames <= 0)
        {
            return HighAverageAwardErrors.InvalidTotalGames;
        }

        if (tournamentsParticipated <= 0)
        {
            return HighAverageAwardErrors.InvalidTournamentsParticipated;
        }

        var id = SeasonAwardId.New();
        return new HighAverageAward
        {
            Id = id,
            BowlerId = bowlerId,
            Average = average,
            TotalGames = totalGames,
            TournamentsParticipated = tournamentsParticipated
        };

    }
}
