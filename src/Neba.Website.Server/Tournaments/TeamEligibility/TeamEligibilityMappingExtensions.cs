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
                : null,
            Summary = response.Summary,
            Members = [.. response.Members.Select(member => member.ToViewModel())],
            Checks = [.. response.Checks.Select(check => new TeamEligibilityCheckViewModel(check.Label, check.Passed))]
        };
    }

    extension(TeamEligibilityMemberResponse member)
    {
        /// <summary>The standing the API named, or null when this site does not know it.</summary>
        public ChampionStandingKind? ParseStanding()
            => Enum.TryParse<ChampionStandingKind>(member.CountsAs, out var standing) && Enum.IsDefined(standing)
                ? standing
                : null;

        public TeamEligibilityMemberViewModel ToViewModel() => new(
            member.Name,
            member.UsbcId,
            member.NotInSystem,
            member.LastTitleYear,
            member.TitlesLast5Years,
            member.TitlesLast10Years,
            member.ParseStanding() ?? ChampionStandingKind.NonChampion,
            member.AgeVerified,
            member.HallOfFameSuperiorPerformance,
            member.TitlesBreakRule,
            member.AgeBreaksRule,
            member.HallOfFameBreaksRule);
    }

    extension(BowlerSearchResponse response)
    {
        public BowlerSearchResultViewModel ToViewModel() => new(response.BowlerId, response.Name, response.UsbcId);
    }
}
