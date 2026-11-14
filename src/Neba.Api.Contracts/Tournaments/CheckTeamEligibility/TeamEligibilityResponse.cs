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
