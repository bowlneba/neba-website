namespace Neba.Website.Server.HallOfFame;

/// <summary>
/// One line of the point table.
/// </summary>
public sealed record HallOfFamePointRuleViewModel
{
    /// <summary>
    /// The title or award name.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// The points earned.
    /// </summary>
    public required int Points { get; init; }
}
