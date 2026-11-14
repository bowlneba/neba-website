using ErrorOr;

using Neba.Api.Contracts.Bowlers;
using Neba.Api.Contracts.Tournaments;
using Neba.Website.Server.Services;

namespace Neba.Website.Server.Tournaments.TeamEligibility;

internal sealed class TeamEligibilityService(
    ApiExecutor executor,
    ITournamentsApi tournamentsApi,
    IBowlersApi bowlersApi) : ITeamEligibilityService
{
    public async Task<ErrorOr<TeamEligibilityResultViewModel>> CheckAsync(
        string tournamentId,
        IReadOnlyCollection<string> bowlerIds,
        int unknownBowlerCount,
        CancellationToken ct = default)
    {
        var result = await executor.ExecuteAsync(
            "TournamentsApi",
            nameof(CheckAsync),
            token => tournamentsApi.CheckTeamEligibilityAsync(tournamentId, bowlerIds, unknownBowlerCount, token),
            ct);

        if (result.IsError)
        {
            return result.Errors;
        }

        // An outcome name this site does not know must not read as "eligible" or "ineligible" by accident.
        // The same goes for a bowler's standing, which the grid shows.
        return Enum.TryParse<TeamEligibilityOutcomeKind>(result.Value.Outcome, out var outcome)
            && Enum.IsDefined(outcome)
            && result.Value.Members.All(member => member.ParseStanding() is not null)
            ? result.Value.ToViewModel(outcome)
            : Error.Failure("TeamEligibility.UnknownOutcome", "The API returned an outcome this site does not know.");
    }

    public async Task<ErrorOr<IReadOnlyList<BowlerSearchResultViewModel>>> SearchBowlersAsync(
        string name, CancellationToken ct = default)
    {
        var result = await executor.ExecuteAsync(
            "BowlersApi",
            nameof(SearchBowlersAsync),
            token => bowlersApi.SearchBowlersAsync(name, token),
            ct);

        if (result.IsError)
        {
            return result.Errors;
        }

        IReadOnlyList<BowlerSearchResultViewModel> bowlers = [.. result.Value.Items.Select(bowler => bowler.ToViewModel())];

        return bowlers.ToErrorOr();
    }
}
