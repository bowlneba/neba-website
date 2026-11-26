namespace Neba.Api.Contracts.Tournaments.ListEligibleBowlers;

/// <summary>
/// What a bowler must meet to be eligible. A null property means that rule does not apply.
/// </summary>
public sealed record EligibilityRequirementsResponse
{
    /// <summary>
    /// The number of tournaments in the season a bowler must have a result in.
    /// </summary>
    /// <example>3</example>
    public int? MinTournamentsCount { get; init; }
}
