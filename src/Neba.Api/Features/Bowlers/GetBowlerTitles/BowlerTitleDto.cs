using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Bowlers.GetBowlerTitles;

/// <summary>
/// Represents a title won by a bowler, including details about the tournament where the title was won.
/// </summary>
public sealed record BowlerTitleDto
{
    /// <summary>
    /// The unique identifier of the tournament where the title was won.
    /// </summary>
    public required TournamentId TournamentId { get; init; }

    /// <summary>
    /// The name of the tournament where the title was won.
    /// </summary>
    public required string TournamentName { get; init; }

    /// <summary>
    /// The date when the tournament was held and the title was won.
    /// </summary>
    public required DateOnly TournamentDate { get; init; }

    /// <summary>
    /// The type of the tournament where the title was won (e.g., "PBA Tour", "PBA Regional", etc.).
    /// </summary>
    public required string TournamentType { get; init; }
}