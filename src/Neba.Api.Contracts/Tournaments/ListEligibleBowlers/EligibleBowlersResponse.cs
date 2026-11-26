namespace Neba.Api.Contracts.Tournaments.ListEligibleBowlers;

/// <summary>
/// An eligibility list and the requirements it was built from.
/// </summary>
public sealed record EligibleBowlersResponse
{
    /// <summary>
    /// The eligibility requirements, or <see langword="null"/> when the tournament has no eligibility list.
    /// </summary>
    public EligibilityRequirementsResponse? Requirements { get; init; }

    /// <summary>
    /// The eligible bowlers, sorted by last then first name. Never null.
    /// </summary>
    public required IReadOnlyCollection<EligibleBowlerSummaryResponse> Bowlers { get; init; }
}