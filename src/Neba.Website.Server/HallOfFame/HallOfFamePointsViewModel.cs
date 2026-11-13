namespace Neba.Website.Server.HallOfFame;

/// <summary>
/// The points page: thresholds, rule table, and the two groups of bowlers.
/// </summary>
public sealed record HallOfFamePointsViewModel
{
    /// <summary>
    /// The thresholds.
    /// </summary>
    public required HallOfFamePointRequirements Requirements { get; init; }

    /// <summary>
    /// The point table, grouped.
    /// </summary>
    public required IReadOnlyCollection<HallOfFamePointRuleGroupViewModel> RuleGroups { get; init; }

    /// <summary>
    /// Bowlers who meet all three criteria, points high to low.
    /// </summary>
    public required IReadOnlyCollection<HallOfFamePointsStandingViewModel> Eligible { get; init; }

    /// <summary>
    /// Everyone else, points high to low.
    /// </summary>
    public required IReadOnlyCollection<HallOfFamePointsStandingViewModel> NotYetEligible { get; init; }
}