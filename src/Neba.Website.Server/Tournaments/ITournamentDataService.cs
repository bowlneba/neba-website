using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Server.Tournaments;

internal interface ITournamentDataService
{
    Task<List<TournamentSummaryViewModel>> GetTournamentsForSeasonAsync(string season, CancellationToken ct = default);
    Task<List<string>> GetAvailableSeasonsAsync(CancellationToken ct = default);
}