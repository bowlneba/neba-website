using Neba.Api.Contracts.Awards;

namespace Neba.Website.Server.History.Awards;

#pragma warning disable S1144 // Remove unused constructor of private type.
#pragma warning disable S2325 // Static types should be used

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
