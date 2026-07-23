namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Determines whether full oil pattern details should be visible for a given tournament
/// and caller, based on the tournament's <see cref="Tournament.OilPatternRevealDateTime"/>.
/// </summary>
internal static class OilPatternRevealPolicy
{
    /// <summary>
    /// Returns <see langword="true"/> when full oil pattern details should be shown: the caller
    /// holds the tournament management permission, there is no reveal date set, or the reveal
    /// date has already passed. Being merely authenticated (without the management permission)
    /// is not sufficient on its own.
    /// </summary>
    public static bool IsRevealed(DateTimeOffset? revealDateTime, bool callerHasTournamentManagementPermission, DateTimeOffset now) =>
        callerHasTournamentManagementPermission || revealDateTime is null || revealDateTime <= now;
}
