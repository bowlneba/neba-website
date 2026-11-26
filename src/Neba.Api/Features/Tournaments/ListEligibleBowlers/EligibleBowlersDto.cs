namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

/// <summary>An eligibility list and the requirements it was built from.</summary>
internal sealed record EligibleBowlersDto
{
    // Null when the tournament has no eligibility list.
    public EligibilityRequirementsDto? Requirements { get; init; }

    public required IReadOnlyCollection<EligibleBowlerSummaryDto> Bowlers { get; init; }
}