namespace Neba.Api.Contracts.Awards;

/// <summary>
/// Data Transfer Object representing the response for a Bowler of the Year award for a season.
/// </summary>
public sealed record BowlerOfTheYearAwardResponse
{
    /// <summary>
    /// Gets the season in which the award was earned.
    /// </summary>
    public required string Season { get; init; }

    /// <summary>
    /// Gets the name of the bowler who received the award.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// Gets the category under which the award was given (e.g., "Open", "Woman", "Senior").
    /// </summary>
    public required string Category { get; init; }
}