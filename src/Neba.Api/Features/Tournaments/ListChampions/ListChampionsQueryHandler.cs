using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Database.Entities;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListChampions;

internal sealed class ListChampionsQueryHandler(AppDbContext appDbContext)
        : IQueryHandler<ListChampionsQuery, IReadOnlyCollection<TournamentChampionsDto>>
{
    // The 2026 NEBA Championship finals were canceled due to a state-of-emergency declaration,
    // so the recorded 1st-place result for that tournament isn't a real title. Excluded by end
    // date until GitHub issue #26 settles how cancellations should be modeled and filtered.
    private static readonly DateOnly CancelledTournamentEndDate = new(2026, 2, 22);

    private readonly IQueryable<HistoricalTournamentChampion> _historicalTournamentChampions = appDbContext.HistoricalTournamentChampions.AsNoTracking();
    private readonly IQueryable<HallOfFameInduction> _hallOfFameInductions = appDbContext.HallOfFameInductions.AsNoTracking();
    private readonly IQueryable<Tournament> _tournaments = appDbContext.Tournaments.AsNoTracking();

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
                HallOfFame = _hallOfFameInductions.Any(induction => induction.BowlerId == tournamentChampion.Bowler.Id)
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

        var recordedChampionsByTournament = await _tournaments
            .Where(tournament => tournament.EndDate != CancelledTournamentEndDate)
            .SelectMany(tournament => tournament.Results
                .Where(result => result.Place == 1)
                .Select(result => new
                {
                    TournamentId = tournament.Id,
                    TournamentName = tournament.Name,
                    TournamentDate = tournament.EndDate,
                    tournament.TournamentType,
                    BowlerId = result.Bowler.Id,
                    BowlerName = result.Bowler.Name,
                    HallOfFame = _hallOfFameInductions.Any(induction => induction.BowlerId == result.Bowler.Id)
                }))
            .GroupBy(champion => champion.TournamentId)
            .ToListAsync(cancellationToken);

        var recordedTournaments = recordedChampionsByTournament.ConvertAll(tournamentChampion => new TournamentChampionsDto
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

        return [.. historicalTournaments, .. recordedTournaments];
    }
}