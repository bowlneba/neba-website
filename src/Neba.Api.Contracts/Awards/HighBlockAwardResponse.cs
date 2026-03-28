namespace Neba.Api.Contracts.Awards;

/// <summary>
/// Data Transfer Object representing the response for a High Block award for a season.
/// </summary>
public sealed record HighBlockAwardResponse
{
    /// <summary>
    /// Gets the unique identifier for this season award.
    /// </summary>
    public required string Season { get; init; }

    /// <summary>
    /// Gets the name of the bowler who received the award.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// Gets the score that qualified for the High Block award.
    /// </summary>
    public required int Score { get; init; }
}