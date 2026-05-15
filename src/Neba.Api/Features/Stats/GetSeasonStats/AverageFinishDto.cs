namespace Neba.Api.Features.Stats.GetSeasonStats;

/// <summary>
/// A leaderboard entry for the Average Finish leaderboard, representing a bowler's mean finishing
/// position across all tournaments in which they received a finishing position. Ordered ascending
/// (lower average finish = better position).
/// </summary>
public sealed record AverageFinishDto
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
    /// The mean finishing position across all tournaments with a recorded finish position.
    /// </summary>
    public required decimal AverageFinish { get; init; }

    /// <summary>
    /// The number of Finals appearances during the season.
    /// </summary>
    public required int Finals { get; init; }

    /// <summary>
    /// Total prize money earned during the season.
    /// </summary>
    public required decimal Winnings { get; init; }
}