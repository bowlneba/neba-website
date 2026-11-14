namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>The combined answer for a tournament.</summary>
/// <param name="Outcome">Eligible, ineligible, or depends on the date.</param>
/// <param name="Reasons">Why the team may not enter; filled only when the outcome is ineligible.</param>
/// <param name="DateChecks">The answer for each date; filled only when the outcome depends on the date.</param>
internal sealed record TeamEligibilityResult(
    TeamEligibilityOutcome Outcome,
    IReadOnlyList<TeamEligibilityReason> Reasons,
    IReadOnlyList<TeamEligibilityCheck> DateChecks);
