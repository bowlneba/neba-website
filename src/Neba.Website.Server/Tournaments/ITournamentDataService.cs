using ErrorOr;

using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Server.Tournaments;

internal interface ITournamentDataService
{
    Task<ErrorOr<List<SeasonTournamentViewModel>>> GetTournamentsForSeasonAsync(SeasonViewModel season, CancellationToken ct = default);
    Task<ErrorOr<List<SeasonViewModel>>> GetSeasonsAsync(CancellationToken ct = default);
}