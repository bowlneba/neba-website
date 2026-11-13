namespace Neba.Website.Server.HallOfFame;

/// <summary>
/// One title or award in a bowler's point breakdown.
/// </summary>
public sealed record HallOfFamePointsEntryViewModel
{
    /// <summary>
    /// The title or award name.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// The year it was earned.
    /// </summary>
    public required int Year { get; init; }

    /// <summary>
    /// The points earned.
    /// </summary>
    public required int Points { get; init; }
}
