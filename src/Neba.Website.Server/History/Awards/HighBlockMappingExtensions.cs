using Neba.Api.Contracts.Awards;

namespace Neba.Website.Server.History.Awards;

internal static class HighBlockMappingExtensions
{
    extension(IEnumerable<HighBlockAwardResponse> responses)
    {
        public IReadOnlyCollection<HighBlockAwardViewModel> ToViewModels()
        {
            return [.. responses
                .GroupBy(r => (r.Season, r.Score))
                .Select(g => new HighBlockAwardViewModel
                {
                    Season = g.Key.Season,
                    Score = g.Key.Score,
                    Bowlers = [.. g.Select(r => r.BowlerName)]
                })];
        }
    }
}