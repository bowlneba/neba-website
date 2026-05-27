namespace Neba.Api.Contracts.Bowlers.GetBowlerTitles;

/// <summary>
/// Represents the response returned by the API when retrieving a bowler's titles, including their name, Hall of Fame status, and a list of titles won with tournament details.
/// </summary>
public sealed record BowlerTitlesResponse
{
    /// <summary>
    /// The full name of the bowler whose titles are being retrieved.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// Indicates whether the bowler is inducted into the Hall of Fame. This is a boolean value where 'true' means the bowler is in the Hall of Fame, and 'false' means they are not.
    /// </summary>
    public required bool HallOfFame { get; init; }

    /// <summary>
    /// A collection of titles won by the bowler, where each title includes details about the tournament in which it was won. This collection may be empty if the bowler has not won any titles.
    /// </summary>
    public required IReadOnlyCollection<BowlerTitleResponse> Titles { get; init; }
}