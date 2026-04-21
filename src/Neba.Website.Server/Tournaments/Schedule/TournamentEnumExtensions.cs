namespace Neba.Website.Server.Tournaments.Schedule;

/// <summary>Human-readable display labels for tournament enum values.</summary>
internal static class TournamentEnumExtensions
{
    /// <summary>Returns a UI-friendly label for a <see cref="TournamentEligibility"/> value.</summary>
    public static string ToDisplayString(this TournamentEligibility eligibility) => eligibility switch
    {
        TournamentEligibility.Open => "Open",
        TournamentEligibility.Senior50Plus => "Senior 50+",
        TournamentEligibility.Women => "Women",
        TournamentEligibility.NonChampions => "Non-Champions",
        _ => eligibility.ToString(),
    };

    /// <summary>Returns a UI-friendly label for a <see cref="TournamentType"/> value.</summary>
    public static string ToDisplayString(this TournamentType type) => type switch
    {
        TournamentType.Singles => "Singles",
        TournamentType.Doubles => "Doubles",
        TournamentType.Trios => "Trios",
        TournamentType.Team => "Team",
        TournamentType.Senior => "Senior",
        TournamentType.Women => "Women",
        TournamentType.SpecialEvent => "Special Event",
        _ => type.ToString(),
    };
}