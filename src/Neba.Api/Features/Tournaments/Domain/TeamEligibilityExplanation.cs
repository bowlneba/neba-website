namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>How a bowler's titles in the champion window rank them.</summary>
internal enum ChampionStanding
{
    NonChampion,
    OneTimeChampion,
    MultiTimeChampion
}

/// <summary>
/// The facts that decided a team's answer for one member, with no date of birth in it.
/// A null value means the fact does not apply to the tournament format.
/// </summary>
/// <param name="Member">The member the facts describe.</param>
/// <param name="TitlesLast5Years">NEBA titles in the 5 years before the date checked.</param>
/// <param name="TitlesLast10Years">NEBA titles in the 10 years before the date checked; null unless the format uses a 10-year window.</param>
/// <param name="CountsAs">How the bowler counts under the rules' own window for that bowler.</param>
/// <param name="LastTitleYear">The year of the bowler's last NEBA title, ever; null when there is none.</param>
/// <param name="AgeVerified">Whether the bowler has an age we can check; null unless the format is over/under.</param>
/// <param name="HallOfFameSuperiorPerformance">Whether the bowler is a Hall of Fame member for Superior Performance; null unless the format is baker.</param>
/// <param name="TitlesBreakRule">True when this bowler's titles count against a champion rule the team failed.</param>
/// <param name="AgeBreaksRule">True when this bowler's missing age counts against the age rule the team failed.</param>
/// <param name="HallOfFameBreaksRule">True when this bowler's Hall of Fame membership counts against the limit the team broke.</param>
internal sealed record TeamMemberExplanation(
    TeamMemberFacts Member,
    int TitlesLast5Years,
    int? TitlesLast10Years,
    ChampionStanding CountsAs,
    int? LastTitleYear,
    bool? AgeVerified,
    bool? HallOfFameSuperiorPerformance,
    bool TitlesBreakRule = false,
    bool AgeBreaksRule = false,
    bool HallOfFameBreaksRule = false);

/// <summary>One rule's tally, such as "Champions: 2 (at most 1)", and whether the team passed it.</summary>
internal readonly record struct TeamEligibilityCheckLine(string Label, bool Passed);

/// <summary>What the rules looked at for a team, shown with every answer.</summary>
/// <param name="Summary">The champion window and the dates checked, in words.</param>
/// <param name="Members">The facts for each member, in the order given.</param>
/// <param name="Checks">The tally of each rule that applies to the format.</param>
internal sealed record TeamEligibilityExplanation(
    string Summary,
    IReadOnlyList<TeamMemberExplanation> Members,
    IReadOnlyList<TeamEligibilityCheckLine> Checks);
