using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Database.Entities;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Bowlers.GetBowlerTitles;

internal sealed class GetBowlerTitlesQueryHandler(AppDbContext appDbContext)
        : IQueryHandler<GetBowlerTitlesQuery, ErrorOr<BowlerTitlesDto>>
{
    private readonly IQueryable<Bowler> _bowlers = appDbContext.Bowlers.AsNoTracking();
    private readonly IQueryable<HallOfFameInduction> _hallOfFameInductions = appDbContext.HallOfFameInductions.AsNoTracking();
    private readonly IQueryable<HistoricalTournamentChampion> _historicalTournamentChampions = appDbContext.HistoricalTournamentChampions.AsNoTracking();
    private readonly IQueryable<Tournament> _tournaments = appDbContext.Tournaments.AsNoTracking();

    public async Task<ErrorOr<BowlerTitlesDto>> HandleAsync(GetBowlerTitlesQuery query, CancellationToken cancellationToken)
    {
        var bowler = await _bowlers
            .Where(bowler => bowler.Id == query.BowlerId)
            .Select(bowler => new
            {
                bowler.Name,
                HallOfFame = _hallOfFameInductions.Any(induction => induction.BowlerId == bowler.Id),
                DbId = EF.Property<int>(bowler, ShadowIdConfiguration.DefaultPropertyName)
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (bowler is null)
        {
            return BowlerErrors.NotFound(query.BowlerId);
        }

        var historicalTitles = await _historicalTournamentChampions
            .Where(tournamentChampion => tournamentChampion.BowlerId == bowler.DbId)
            .Select(tournamentChampion => new
            {
                TournamentId = tournamentChampion.Tournament.Id,
                TournamentName = tournamentChampion.Tournament.Name,
                TournamentDate = tournamentChampion.Tournament.EndDate,
                tournamentChampion.Tournament.TournamentType
            })
            .ToListAsync(cancellationToken);

        var recordedTitles = await _tournaments
            .Where(tournament => tournament.TitleEligible)
            .SelectMany(tournament => tournament.Results
                .Where(result => result.BowlerId == query.BowlerId && result.Place == 1)
                .Select(_ => new
                {
                    TournamentId = tournament.Id,
                    TournamentName = tournament.Name,
                    TournamentDate = tournament.EndDate,
                    tournament.TournamentType
                }))
            .ToListAsync(cancellationToken);

        return new BowlerTitlesDto
        {
            BowlerName = bowler.Name,
            HallOfFame = bowler.HallOfFame,
            Titles = [.. historicalTitles.Concat(recordedTitles).Select(title => new BowlerTitleDto
            {
                TournamentId = title.TournamentId,
                TournamentName = title.TournamentName,
                TournamentDate = title.TournamentDate,
                TournamentType = title.TournamentType.Name
            })]
        };
    }
}