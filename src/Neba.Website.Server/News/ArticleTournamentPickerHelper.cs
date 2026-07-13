using Neba.Website.Server.Tournaments;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Server.News;

/// <summary>
/// Shared "pick a season, default to the current one, load its tournaments" logic used by both
/// <see cref="CreateArticle"/> and <see cref="EditArticle"/> when the article isn't already
/// deep-linked/assigned to a specific tournament.
/// </summary>
internal static class ArticleTournamentPickerHelper
{
    public static async Task<(List<SeasonViewModel> Seasons, string? SelectedSeasonId, List<SeasonTournamentViewModel> Tournaments)>
        LoadDefaultSeasonAsync(ITournamentApiService tournamentApiService)
    {
        var seasonsResult = await tournamentApiService.GetSeasonsAsync();

        if (seasonsResult.IsError || seasonsResult.Value.Count == 0)
        {
            return ([], null, []);
        }

        var seasons = seasonsResult.Value;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentSeason = seasons.Find(s => today >= s.StartDate && today <= s.EndDate) ?? seasons[0];

        var tournamentsResult = await tournamentApiService.GetTournamentsForSeasonAsync(currentSeason);
        var tournaments = tournamentsResult.IsError ? [] : tournamentsResult.Value;

        return (seasons, currentSeason.Id, tournaments);
    }
}
