using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.CheckTeamEligibility;

internal sealed record TeamEligibilityDto(
    TeamEligibilityOutcome Outcome,
    IReadOnlyList<TeamEligibilityReason> Reasons,
    IReadOnlyList<TeamEligibilityCheck> DateChecks,
    DefendingTeam? DefendingTeam = null);
