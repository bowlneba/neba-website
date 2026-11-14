using Neba.Api.Features.Tournaments;
using Neba.Api.Features.Tournaments.CheckTeamEligibility;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Tournaments;

public static class TeamEligibilityDtoFactory
{
    internal static TeamEligibilityDto Create(
        TeamEligibilityOutcome? outcome = null,
        IReadOnlyList<TeamEligibilityReason>? reasons = null,
        IReadOnlyList<TeamEligibilityCheck>? dateChecks = null,
        DefendingTeam? defendingTeam = null,
        TeamEligibilityExplanation? explanation = null,
        IReadOnlyList<TeamEligibilityMemberIdentity>? memberIdentities = null)
        => new(
            outcome ?? TeamEligibilityOutcome.Eligible,
            reasons ?? [],
            dateChecks ?? [],
            defendingTeam,
            explanation,
            memberIdentities);
}
