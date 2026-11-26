namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

/// <summary>What a bowler must meet to be eligible. A null property means that rule does not apply.</summary>
internal sealed record EligibilityRequirementsDto
{
    public int? MinTournamentsCount { get; init; }
}
