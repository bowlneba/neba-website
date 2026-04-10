using Neba.Api.Contracts.Awards;

namespace Neba.Website.Server.History.Awards;

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