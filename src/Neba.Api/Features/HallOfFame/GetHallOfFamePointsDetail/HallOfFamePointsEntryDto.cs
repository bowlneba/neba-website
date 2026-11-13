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

    /// <summary>
    /// The bowler's points after this entry, counting oldest first.
    /// </summary>
    public required int RunningTotal { get; init; }

    /// <summary>
    /// True only for the entry that first takes the running total to the required points.
    /// </summary>
    public required bool ReachedThreshold { get; init; }
}
