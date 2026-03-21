using Neba.Domain.Bowlers;

namespace Neba.Domain.Awards;

/// <summary>
/// Recognizes overall performance across Stat-Eligible Tournaments during the Season.
/// A separate record exists for each <see cref="BowlerOfTheYearCategory"/> a bowler wins within a season.
/// </summary>
public sealed class BowlerOfTheYearAward
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
    /// The category in which the award is given.
    /// Age eligibility is evaluated as of each tournament date during the season.
    /// </summary>
    public required BowlerOfTheYearCategory Category { get; init; }

    /// <summary>
    /// Navigation to the bowler. Internal — for EF Core query projections only.
    /// </summary>
    internal Bowler Bowler { get; init; } = null!;
}
