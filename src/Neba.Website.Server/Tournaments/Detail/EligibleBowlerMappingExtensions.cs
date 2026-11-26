using Neba.Api.Contracts.Tournaments.ListEligibleBowlers;

namespace Neba.Website.Server.Tournaments.Detail;

internal static class EligibleBowlerMappingExtensions
{
    extension(EligibleBowlersResponse response)
    {
        public EligibleBowlersViewModel ToViewModel() => new()
        {
            ListApplies = response.Requirements is not null,
            MinTournamentsCount = response.Requirements?.MinTournamentsCount,
            Bowlers = [.. response.Bowlers.Select(bowler => bowler.ToViewModel())]
        };
    }

    extension(EligibleBowlerSummaryResponse response)
    {
        public EligibleBowlerViewModel ToViewModel() => new()
        {
            Name = response.Name,
            UsbcId = response.UsbcId
        };
    }
}