namespace Neba.Website.Server.Tournaments.TeamEligibility;

/// <summary>The outcomes of a team eligibility check, as the API names them.</summary>
public enum TeamEligibilityOutcomeKind
{
    /// <summary>The team is a defending team, so the champion limits do not apply.</summary>
    DefendingChampions,

    /// <summary>The team may enter on every date of the tournament.</summary>
    Eligible,

    /// <summary>The team may not enter.</summary>
    Ineligible,

    /// <summary>The team may enter on some dates of the tournament but not others.</summary>
    DependsOnDate
}

/// <summary>The answer to "may this team enter this tournament".</summary>
public sealed record TeamEligibilityResultViewModel
{
    /// <summary>The answer.</summary>
    public required TeamEligibilityOutcomeKind Outcome { get; init; }

    /// <summary>Why the team may not enter; filled only for an ineligible team.</summary>
    public IReadOnlyList<string> Reasons { get; init; } = [];

    /// <summary>The answer for each date; filled only when the answer depends on the date.</summary>
    public IReadOnlyList<TeamEligibilityDateCheckViewModel> DateChecks { get; init; } = [];

    /// <summary>True when PBA and PWBA titles were checked; the API always says false today.</summary>
    public bool ProfessionalTitlesVerified { get; init; }

    /// <summary>The tournament a defending team won; filled only for defending champions.</summary>
    public DefendingTeamViewModel? DefendingSource { get; init; }

    /// <summary>The champion window and the dates checked, in words; empty for defending champions.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The facts behind the answer for each team member; empty for defending champions.</summary>
    public IReadOnlyList<TeamEligibilityMemberViewModel> Members { get; init; } = [];

    /// <summary>The tally of each rule that applies to the format.</summary>
    public IReadOnlyList<TeamEligibilityCheckViewModel> Checks { get; init; } = [];

    /// <summary>The pro-title note applies whenever the team passed on NEBA data.</summary>
    public bool ShowProfessionalTitlesNote =>
        !ProfessionalTitlesVerified
        && (Outcome == TeamEligibilityOutcomeKind.Eligible
            || (Outcome == TeamEligibilityOutcomeKind.DependsOnDate && DateChecks.Any(check => check.Eligible)));
}

/// <summary>The answer for one date of the tournament.</summary>
public sealed record TeamEligibilityDateCheckViewModel(DateOnly AsOf, bool Eligible, IReadOnlyList<string> Reasons);

/// <summary>How a bowler's titles rank them, as the API names it.</summary>
public enum ChampionStandingKind
{
    /// <summary>No title in the champion window.</summary>
    NonChampion,

    /// <summary>One title in the champion window.</summary>
    OneTimeChampion,

    /// <summary>More than one title in the champion window.</summary>
    MultiTimeChampion
}

/// <summary>The facts behind the answer for one team member. A null fact does not apply to the format.</summary>
public sealed record TeamEligibilityMemberViewModel(
    string? Name,
    string? UsbcId,
    bool NotInSystem,
    int? LastTitleYear,
    int TitlesLast5Years,
    int? TitlesLast10Years,
    ChampionStandingKind CountsAs,
    bool? AgeVerified,
    bool? HallOfFameSuperiorPerformance,
    bool TitlesBreakRule,
    bool AgeBreaksRule,
    bool HallOfFameBreaksRule);

/// <summary>The tally of one rule and whether the team passed it.</summary>
public sealed record TeamEligibilityCheckViewModel(string Label, bool Passed);
