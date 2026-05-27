using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Bowlers.GetBowlerTitles;

/// <summary>
/// Represents the titles won by a bowler, including the bowler's name, Hall of Fame status, and a list of titles with tournament details.
/// </summary>
public sealed record BowlerTitlesDto
{
    /// <summary>
    /// The name of the bowler whose titles are being represented.
    /// </summary>
    public required Name BowlerName { get; init; }

    /// <summary>
    /// Indicates whether the bowler is a member of the Hall of Fame.
    /// </summary>
    public required bool HallOfFame { get; init; }

    /// <summary>
    /// A collection of titles won by the bowler, where each title includes details about the tournament where it was won.
    /// </summary>
    public required IReadOnlyCollection<BowlerTitleDto> Titles { get; init; }
}