namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>The answer for one date.</summary>
/// <param name="AsOf">The date the rules were run for.</param>
/// <param name="Reasons">Why the team may not enter on that date; empty when it may.</param>
/// <param name="CompositionFailed">True when the champion and Hall of Fame rules failed, so the age rules never ran.</param>
internal sealed record TeamEligibilityCheck(DateOnly AsOf, IReadOnlyList<TeamEligibilityReason> Reasons, bool CompositionFailed = false)
{
    /// <summary>True when the team may enter on <see cref="AsOf"/>.</summary>
    public bool Eligible => Reasons.Count == 0;
}
