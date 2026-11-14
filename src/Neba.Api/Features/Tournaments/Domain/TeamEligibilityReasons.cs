namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// All team eligibility reason text lives here, so a wording review touches one file.
/// Age reasons never name a bowler or give a date of birth.
/// </summary>
internal static class TeamEligibilityReasons
{
    // The wording below is a placeholder until it has been reviewed.

    /// <summary>
    /// A bowler has no date of birth on file, or is not in the system. The text never says which bowler or why.
    /// </summary>
    public static readonly TeamEligibilityReason AgeNotVerified = new(
        "Age.NotVerified",
        "We could not verify the age of every bowler. An over/under team needs every bowler found in our system with an age we can check.");

    /// <summary>Both bowlers are in the same age group. The text does not say which group.</summary>
    public static readonly TeamEligibilityReason SameAgeGroup = new(
        "Age.SameAgeGroup",
        "An over/under team needs one bowler in each age group. Both bowlers on this team are in the same group.");

    /// <summary>A team needs at least one bowler who has not won a title in the window.</summary>
    public static readonly TeamEligibilityReason NoNonChampion = new(
        "Champions.NoNonChampion",
        "A team needs at least one non-champion.");

    /// <summary>A baker team may have only one Hall of Fame member for Superior Performance.</summary>
    public static readonly TeamEligibilityReason TooManyHallOfFame = new(
        "HallOfFame.TooMany",
        "A team may have at most one Hall of Fame member for Superior Performance.");

    /// <summary>More champions than the format allows.</summary>
    public static TeamEligibilityReason TooManyChampions(int max)
        => new("Champions.TooMany", $"A team may have at most {max} previous champion{Plural(max)}.");

    /// <summary>More multi-time champions than the format allows.</summary>
    public static TeamEligibilityReason TooManyMultiTimeChampions(int max)
        => new("Champions.TooManyMultiTime", $"A team may have at most {max} multi-time champion{Plural(max)}.");

    // Wording for the facts shown with every answer (also placeholder until reviewed).

    /// <summary>Describes the champion window and the dates checked.</summary>
    public static string WindowSummary(string tournamentName, int years, bool seniorWindow, string dates)
        => $"For {tournamentName}, a champion is a bowler with a NEBA title in the {years} years before the tournament"
            + (seniorWindow ? " (5 years once a bowler has turned 60)" : string.Empty)
            + $". {dates}";

    /// <summary>"Champions: 2 (at most 1)".</summary>
    public static string ChampionsCheck(int count, int max) => $"Champions: {count} (at most {max})";

    /// <summary>"Non-champions: 1 (needs at least 1)".</summary>
    public static string NonChampionsCheck(int count) => $"Non-champions: {count} (needs at least 1)";

    /// <summary>"Multi-time champions: 2 (at most 1)".</summary>
    public static string MultiTimeCheck(int count, int max) => $"Multi-time champions: {count} (at most {max})";

    /// <summary>"Hall of Fame members for Superior Performance: 2 (at most 1)".</summary>
    public static string HallOfFameCheck(int count) => $"Hall of Fame members for Superior Performance: {count} (at most 1)";

    /// <summary>"Bowlers with an age we can check: 1 of 2 (needs all)".</summary>
    public static string AgeCheck(int verified, int total) => $"Bowlers with an age we can check: {verified} of {total} (needs all)";

    private static string Plural(int count)
        => count == 1 ? string.Empty : "s";
}