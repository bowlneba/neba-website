namespace Neba.Website.Server.HallOfFame;

/// <summary>
/// A section of the point table.
/// </summary>
public sealed record HallOfFamePointRuleGroupViewModel
{
    /// <summary>
    /// The section heading.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The rules in the section, in display order.
    /// </summary>
    public required IReadOnlyCollection<HallOfFamePointRuleViewModel> Rules { get; init; }
}
