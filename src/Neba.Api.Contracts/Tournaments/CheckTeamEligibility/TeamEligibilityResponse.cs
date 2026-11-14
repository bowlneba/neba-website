namespace Neba.Api.Contracts.Tournaments.CheckTeamEligibility;

/// <summary>
/// Whether a team may enter a tournament under Rules 401 to 403, and why not when it may not.
/// </summary>
public sealed record TeamEligibilityResponse
{
    /// <summary>
    /// The answer: <c>DefendingChampions</c>, <c>Eligible</c>, <c>Ineligible</c>, or <c>DependsOnDate</c>.
    /// </summary>
    /// <example>Eligible</example>
    public required string Outcome { get; init; }

    /// <summary>
    /// Why the team may not enter; filled only when <see cref="Outcome"/> is <c>Ineligible</c>.
    /// </summary>
    public IReadOnlyCollection<TeamEligibilityReasonResponse> Reasons { get; init; } = [];

    /// <summary>
    /// The answer for each date of the tournament; filled only when <see cref="Outcome"/> is <c>DependsOnDate</c>.
    /// </summary>
    public IReadOnlyCollection<TeamEligibilityDateCheckResponse> DateChecks { get; init; } = [];

    /// <summary>
    /// Always <see langword="false"/> today: PBA and PWBA titles are not checked.
    /// </summary>
    public bool ProfessionalTitlesVerified { get; init; }

    /// <summary>
    /// The tournament this team won; filled only when <see cref="Outcome"/> is <c>DefendingChampions</c>.
    /// </summary>
    public DefendingSourceResponse? DefendingSource { get; init; }

    /// <summary>
    /// The champion window and the dates checked, in words; empty when <see cref="Outcome"/> is <c>DefendingChampions</c>.
    /// </summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    /// The facts behind the answer for each team member, in the order the bowlers were given with members
    /// not in the system last; empty when <see cref="Outcome"/> is <c>DefendingChampions</c>. Counts are as of
    /// the tournament's start date. Never carries a date of birth.
    /// </summary>
    public IReadOnlyCollection<TeamEligibilityMemberResponse> Members { get; init; } = [];

    /// <summary>
    /// The tally of each rule that applies to the format, such as "Champions: 2 (at most 1)".
    /// </summary>
    public IReadOnlyCollection<TeamEligibilityCheckResponse> Checks { get; init; } = [];
}

/// <summary>
/// The facts behind the answer for one team member. A null fact does not apply to the tournament format.
/// </summary>
public sealed record TeamEligibilityMemberResponse
{
    /// <summary>
    /// The bowler's display name; null for a member not in the system.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The bowler's USBC membership ID, when known.
    /// </summary>
    public string? UsbcId { get; init; }

    /// <summary>
    /// True for a member not in the system.
    /// </summary>
    public bool NotInSystem { get; init; }

    /// <summary>
    /// The year of the bowler's last NEBA title; null when there is none.
    /// </summary>
    public int? LastTitleYear { get; init; }

    /// <summary>
    /// NEBA titles in the 5 years before the tournament's start date.
    /// </summary>
    public int TitlesLast5Years { get; init; }

    /// <summary>
    /// NEBA titles in the 10 years before the start date; null unless the format uses a 10-year window.
    /// </summary>
    public int? TitlesLast10Years { get; init; }

    /// <summary>
    /// <c>NonChampion</c>, <c>OneTimeChampion</c> or <c>MultiTimeChampion</c>.
    /// </summary>
    public required string CountsAs { get; init; }

    /// <summary>
    /// Whether the bowler has an age we can check; null unless the format is over/under.
    /// </summary>
    public bool? AgeVerified { get; init; }

    /// <summary>
    /// Whether the bowler is a Hall of Fame member for Superior Performance; null unless the format is baker.
    /// </summary>
    public bool? HallOfFameSuperiorPerformance { get; init; }

    /// <summary>
    /// True when this bowler's titles count against a champion rule the team failed.
    /// </summary>
    public bool TitlesBreakRule { get; init; }

    /// <summary>
    /// True when this bowler's missing age counts against the age rule the team failed.
    /// </summary>
    public bool AgeBreaksRule { get; init; }

    /// <summary>
    /// True when this bowler's Hall of Fame membership counts against the limit the team broke.
    /// </summary>
    public bool HallOfFameBreaksRule { get; init; }
}

/// <summary>
/// The tally of one rule.
/// </summary>
public sealed record TeamEligibilityCheckResponse
{
    /// <summary>
    /// The tally in words, such as "Champions: 2 (at most 1)".
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Whether the team passed the rule.
    /// </summary>
    public required bool Passed { get; init; }
}

/// <summary>
/// The tournament a defending team won.
/// </summary>
public sealed record DefendingSourceResponse
{
    /// <summary>
    /// The name of the tournament the team won.
    /// </summary>
    public required string TournamentName { get; init; }

    /// <summary>
    /// The season of the tournament the team won.
    /// </summary>
    public required string SeasonDescription { get; init; }

    /// <summary>
    /// The tournament this team won was truncated.
    /// </summary>
    public bool SourceTruncated { get; init; }

    /// <summary>
    /// The team won an earlier tournament and could not defend because a later one was truncated.
    /// </summary>
    public bool Undefended { get; init; }
}

/// <summary>
/// One reason a team may not enter.
/// </summary>
public sealed record TeamEligibilityReasonResponse
{
    /// <summary>
    /// A stable code for the reason, such as <c>Champions.TooMany</c>.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// The text to show the user.
    /// </summary>
    public required string Message { get; init; }
}

/// <summary>
/// The answer for one date of the tournament.
/// </summary>
public sealed record TeamEligibilityDateCheckResponse
{
    /// <summary>
    /// The date the rules were checked for.
    /// </summary>
    public required DateOnly AsOf { get; init; }

    /// <summary>
    /// Whether the team may enter on that date.
    /// </summary>
    public required bool Eligible { get; init; }

    /// <summary>
    /// Why the team may not enter on that date; empty when it may.
    /// </summary>
    public IReadOnlyCollection<TeamEligibilityReasonResponse> Reasons { get; init; } = [];
}
