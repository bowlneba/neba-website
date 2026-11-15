using Ardalis.SmartEnum;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>The answer to "may this team enter this tournament".</summary>
internal sealed class TeamEligibilityOutcome
    : SmartEnum<TeamEligibilityOutcome>
{
    /// <summary>The team is a defending team, so the champion limits do not apply.</summary>
    public static readonly TeamEligibilityOutcome DefendingChampions = new(nameof(DefendingChampions), 1);

    /// <summary>The team may enter on every date of the tournament.</summary>
    public static readonly TeamEligibilityOutcome Eligible = new(nameof(Eligible), 2);

    /// <summary>The team may not enter on any date of the tournament.</summary>
    public static readonly TeamEligibilityOutcome Ineligible = new(nameof(Ineligible), 3);

    /// <summary>The team may enter on some dates of the tournament but not others.</summary>
    public static readonly TeamEligibilityOutcome DependsOnDate = new(nameof(DependsOnDate), 4);

    private TeamEligibilityOutcome(string name, int value)
        : base(name, value)
    {
    }
}