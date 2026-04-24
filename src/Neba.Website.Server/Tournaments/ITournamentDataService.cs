using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Server.Tournaments;

internal interface ITournamentDataService
{
    Task<List<SeasonTournamentViewModel>> GetTournamentsForSeasonAsync(SeasonViewModel season, CancellationToken ct = default);
    Task<List<SeasonViewModel>> GetSeasonsAsync(CancellationToken ct = default);
}