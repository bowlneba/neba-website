namespace Neba.Api.Contracts.Bowlers.GetBowlerTitles;

/// <summary>
/// Represents a single title won by a bowler, including the tournament details.
/// </summary>
public sealed record BowlerTitleResponse
{
    /// <summary>
    /// The unique identifier of the tournament where the title was won.
    /// </summary>
    public required string TournamentId { get; init; }

    /// <summary>
    /// The name of the tournament where the title was won.
    /// </summary>
    public required string TournamentName { get; init; }

    /// <summary>
    /// The date when the tournament was held, which is also the date when the title was won.
    /// </summary>
    public required DateOnly TournamentDate { get; init; }

    /// <summary>
    /// The type of the tournament (e.g., "Singles", "Doubles", "Tournament of Champions", etc.) where the title was won.
    /// </summary>
    public required string TournamentType { get; init; }
}