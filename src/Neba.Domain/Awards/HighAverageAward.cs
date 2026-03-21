using ErrorOr;

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
        if (bowlerId == BowlerId.Empty)
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

        var award = new HighAverageAward
        {
            Id = SeasonAwardId.New(),
            BowlerId = bowlerId,
            Average = average,
            TotalGames = totalGames,
            TournamentsParticipated = tournamentsParticipated
        };

        return award;

    }
}

internal static class HighAverageAwardErrors
{
    public static readonly Error BowlerIdRequired = Error.Validation(
        code: "HighAverageAward.BowlerIdRequired",
        description: "Bowler ID is required.");

    public static readonly Error InvalidAverage = Error.Validation(
        code: "HighAverageAward.InvalidAverage",
        description: "Average must be greater than zero.");

    public static readonly Error InvalidTotalGames = Error.Validation(
        code: "HighAverageAward.InvalidTotalGames",
        description: "Total games must be greater than zero.");

    public static readonly Error InvalidTournamentsParticipated = Error.Validation(
        code: "HighAverageAward.InvalidTournamentsParticipated",
        description: "Tournaments participated must be greater than zero.");
}