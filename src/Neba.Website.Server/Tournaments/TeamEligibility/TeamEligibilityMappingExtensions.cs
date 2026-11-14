using Neba.Api.Contracts.Bowlers.SearchBowlers;
using Neba.Api.Contracts.Tournaments.CheckTeamEligibility;

namespace Neba.Website.Server.Tournaments.TeamEligibility;

internal static class TeamEligibilityMappingExtensions
{
    extension(TeamEligibilityResponse response)
    {
        public TeamEligibilityResultViewModel ToViewModel(TeamEligibilityOutcomeKind outcome) => new()
        {
            Outcome = outcome,
            Reasons = [.. response.Reasons.Select(reason => reason.Message)],
            DateChecks =
            [
                .. response.DateChecks.Select(check => new TeamEligibilityDateCheckViewModel(
                    check.AsOf, check.Eligible, [.. check.Reasons.Select(reason => reason.Message)]))
            ],
            ProfessionalTitlesVerified = response.ProfessionalTitlesVerified,
            DefendingSource = response.DefendingSource is { } source
                ? new DefendingTeamViewModel(
                    source.TournamentName, source.SeasonDescription, source.SourceTruncated, source.Undefended, [])
                : null
        };
    }

    extension(BowlerSearchResponse response)
    {
        public BowlerSearchResultViewModel ToViewModel() => new(response.BowlerId, response.Name, response.UsbcId);
    }
}
