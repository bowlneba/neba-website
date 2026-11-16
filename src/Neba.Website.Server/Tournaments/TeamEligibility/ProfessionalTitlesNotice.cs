namespace Neba.Website.Server.Tournaments.TeamEligibility;

/// <summary>Shared wording for the notice that PBA and PWBA titles are not checked.</summary>
internal static class ProfessionalTitlesNotice
{
    /// <summary>The lead sentence shown before and after a check.</summary>
    public const string Headline = "We can't verify PBA or PWBA titles";

    /// <summary>The team's duty, shown before and after a check.</summary>
    public const string Responsibility =
        "Your team is responsible for making sure everyone is eligible to bowl together, "
        + "including any professional titles inside the champion window. "
        + "A result here does not confirm eligibility.";

    /// <summary>The full text of the pre-check alert.</summary>
    public const string AlertMessage =
        "This tool checks NEBA titles, age, and Hall of Fame membership only. "
        + "It has no PBA or PWBA title records. " + Responsibility;
}