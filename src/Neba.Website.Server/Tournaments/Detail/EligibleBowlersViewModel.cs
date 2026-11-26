namespace Neba.Website.Server.Tournaments.Detail;

/// <summary>The bowlers eligible for a tournament and the rule the list was built from.</summary>
public sealed record EligibleBowlersViewModel
{
    /// <summary>False when the tournament has no eligibility list.</summary>
    public required bool ListApplies { get; init; }

    /// <summary>Tournaments a bowler must have a result in; null when no such rule applies.</summary>
    public int? MinTournamentsCount { get; init; }

    /// <summary>The eligible bowlers, in the order the API returned them.</summary>
    public required IReadOnlyList<EligibleBowlerViewModel> Bowlers { get; init; }
}