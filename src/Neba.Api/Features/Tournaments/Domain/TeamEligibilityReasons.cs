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

    private static string Plural(int count)
        => count == 1 ? string.Empty : "s";
}
