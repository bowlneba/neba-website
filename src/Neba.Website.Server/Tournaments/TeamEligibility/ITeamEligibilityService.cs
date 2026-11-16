using ErrorOr;

namespace Neba.Website.Server.Tournaments.TeamEligibility;

internal interface ITeamEligibilityService
{
    /// <summary>
    /// Checks whether the team is eligible to bowl. <paramref name="unknownBowlerCount"/> counts members not found
    /// in the system.
    /// </summary>
    Task<ErrorOr<TeamEligibilityResultViewModel>> CheckAsync(
        string tournamentId,
        IReadOnlyCollection<string> bowlerIds,
        int unknownBowlerCount,
        CancellationToken ct = default);

    /// <summary>Searches bowlers by name; the API needs at least 3 characters and returns at most 20.</summary>
    Task<ErrorOr<IReadOnlyList<BowlerSearchResultViewModel>>> SearchBowlersAsync(
        string name, CancellationToken ct = default);
}