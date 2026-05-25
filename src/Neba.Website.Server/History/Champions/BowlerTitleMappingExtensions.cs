using System.Globalization;

using Neba.Api.Contracts.Bowlers.GetBowlerTitles;
using Neba.Api.Contracts.Tournaments.ListChampions;

namespace Neba.Website.Server.History.Champions;

internal static class BowlerTitleMappingExtensions
{
    extension(IReadOnlyCollection<TournamentChampionResponse> responses)
    {
        public List<BowlerTitleSummaryViewModel> ToTitleSummaries()
        {
            return [.. responses
                .SelectMany(t => t.Champions.Select(c => (Champion: c, Tournament: t)))
                .GroupBy(pair => pair.Champion.BowlerId)
                .Select(g => new BowlerTitleSummaryViewModel
                {
                    BowlerId = g.Key,
                    BowlerName = g.First().Champion.BowlerName,
                    TitleCount = g.Count(),
                    HallOfFame = g.First().Champion.HallOfFame,
                })
                .OrderByDescending(s => s.TitleCount)];
        }

        public List<TitlesByYearViewModel> ToTitlesByYear()
        {
            return [.. responses
                .GroupBy(t => t.TournamentDate.Year)
                .OrderByDescending(g => g.Key)
                .Select(g => new TitlesByYearViewModel
                {
                    Year = g.Key,
                    Titles = [.. g.SelectMany(t => t.Champions.Select(c => new BowlerTitleViewModel
                    {
                        BowlerId = c.BowlerId,
                        BowlerName = c.BowlerName,
                        TournamentId = t.TournamentId,
                        TournamentMonth = t.TournamentDate.Month,
                        TournamentYear = t.TournamentDate.Year,
                        TournamentType = t.TournamentType,
                        HallOfFame = c.HallOfFame,
                    }))],
                })];
        }
    }

    extension(BowlerTitlesResponse response)
    {
        public BowlerTitlesViewModel ToViewModel()
        {
            var titles = response.Titles
                .OrderByDescending(t => t.TournamentDate)
                .Select(t => new TitleViewModel
                {
                    TournamentId = t.TournamentId,
                    TournamentName = t.TournamentName,
                    TournamentDate = t.TournamentDate.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    TournamentType = t.TournamentType,
                })
                .ToList();

            return new BowlerTitlesViewModel
            {
                BowlerName = response.BowlerName,
                HallOfFame = response.HallOfFame,
                Titles = titles,
            };
        }
    }
}