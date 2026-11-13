namespace Neba.Api.Features.HallOfFame.GetHallOfFamePointsDetail;

/// <summary>
/// One title or award that earned Hall of Fame points.
/// </summary>
public sealed record HallOfFamePointsEntryDto
{
    /// <summary>
    /// The title or award name.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// When the title was won, or the end of the season for an award.
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// The points earned.
    /// </summary>
    public required int Points { get; init; }
}
