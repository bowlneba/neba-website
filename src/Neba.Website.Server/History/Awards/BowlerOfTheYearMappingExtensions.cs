using Neba.Api.Contracts.Awards;

namespace Neba.Website.Server.History.Awards;

#pragma warning disable S1144 // Remove unused constructor of private type.
#pragma warning disable S2325 // Static types should be used

internal static class BowlerOfTheYearMappingExtensions
{
    // Controls category display order within each season card.
    private static readonly Dictionary<string, int> CategoryOrder = new()
    {
        ["Open"] = 1,
        ["Senior"] = 2,
        ["Super Senior"] = 3,
        ["Woman"] = 4,
        ["Rookie"] = 5,
        ["Youth"] = 6,
    };

    extension(IEnumerable<BowlerOfTheYearAwardResponse> responses)
    {
        public IReadOnlyList<BowlerOfTheYearByYearViewModel> ToViewModels()
        {
            return [.. responses
                .GroupBy(r => r.Season)
                .OrderByDescending(g => g.Key)
                .Select(g => new BowlerOfTheYearByYearViewModel
                {
                    Season = g.Key,
                    WinnersByCategory = [.. g
                        .OrderBy(r => CategoryOrder.GetValueOrDefault(r.Category, int.MaxValue))
                        .Select(r => new KeyValuePair<string, string>(
                            GetDisplayCategory(r.Category),
                            r.BowlerName))]
                })];
        }
    }

    private static string GetDisplayCategory(string category) =>
        category == "Open" ? "Bowler of the Year" : category;
}