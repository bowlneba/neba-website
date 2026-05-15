namespace Neba.Api.Features.Awards.ListBowlerOfTheYearAwards;

/// <summary>
/// Data Transfer Object representing the Bowler of the Year Award, which recognizes overall performance
/// across Stat-Eligible Tournaments during a Season. A separate record exists for each category a bowler wins.
/// </summary>
public sealed record BowlerOfTheYearAwardDto
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
    /// The category under which the award was given (e.g., "Open", "Woman", "Senior").
    /// </summary>
    public required string Category { get; init; }
}