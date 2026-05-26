using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Database.Entities;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListChampions;

internal sealed class ListChampionsQueryHandler(AppDbContext appDbContext)
        : IQueryHandler<ListChampionsQuery, IReadOnlyCollection<TournamentChampionsDto>>
{
    private readonly IQueryable<HistoricalTournamentChampion> _historicalTournamentChampions = appDbContext.HistoricalTournamentChampions.AsNoTracking();

    public async Task<IReadOnlyCollection<TournamentChampionsDto>> HandleAsync(ListChampionsQuery query, CancellationToken cancellationToken)
    {
        var historicalChampionsByTournament = await _historicalTournamentChampions
            .Select(tournamentChampion => new
            {
                TournamentId = tournamentChampion.Tournament.Id,
                TournamentName = tournamentChampion.Tournament.Name,
                TournamentDate = tournamentChampion.Tournament.EndDate,
                tournamentChampion.Tournament.TournamentType,
                BowlerId = tournamentChampion.Bowler.Id,
                BowlerName = tournamentChampion.Bowler.Name,
                HallOfFame = tournamentChampion.Bowler.HallOfFameInductions.Any()
            })
            .GroupBy(tournamentChampion => tournamentChampion.TournamentId)
            .ToListAsync(cancellationToken);

        var historicalTournaments = historicalChampionsByTournament.ConvertAll(tournamentChampion => new TournamentChampionsDto
        {
            TournamentId = tournamentChampion.Key,
            TournamentName = tournamentChampion.First().TournamentName,
            TournamentDate = tournamentChampion.First().TournamentDate,
            TournamentType = tournamentChampion.First().TournamentType.Name,
            Champions = [.. tournamentChampion.Select(champion => new ChampionDto
            {
                BowlerId = champion.BowlerId,
                BowlerName = champion.BowlerName,
                HallOfFame = champion.HallOfFame
            })]
        });

        // will union with full-stat tournament winners in a future iteration

        return [.. historicalTournaments];
    }
}