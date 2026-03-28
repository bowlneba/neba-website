using Neba.Api.Contracts.Awards;

namespace Neba.Website.Server.History.Awards;

#pragma warning disable S1144 // Remove unused constructor of private type.
#pragma warning disable S2325 // Static types should be used

internal static class HighAverageMappingExtensions
{
    extension(HighAverageAwardResponse response)
    {
        public HighAverageAwardViewModel ToViewModel()
        {
            return new HighAverageAwardViewModel
            {
                Season = response.Season,
                BowlerName = response.BowlerName,
                Average = response.Average,
                TotalGames = response.TotalGames,
                TournamentsParticipated = response.TournamentsParticipated
            };
        }
    }
}