using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Database.Entities;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Bowlers.GetBowlerTitles;

internal sealed class GetBowlerTitlesQueryHandler(AppDbContext appDbContext)
        : IQueryHandler<GetBowlerTitlesQuery, ErrorOr<BowlerTitlesDto>>
{
    private readonly IQueryable<Bowler> _bowlers = appDbContext.Bowlers.AsNoTracking();
    private readonly IQueryable<HistoricalTournamentChampion> _historicalTournamentChampions = appDbContext.HistoricalTournamentChampions.AsNoTracking();

    public async Task<ErrorOr<BowlerTitlesDto>> HandleAsync(GetBowlerTitlesQuery query, CancellationToken cancellationToken)
    {
        var bowler = await _bowlers
            .Where(bowler => bowler.Id == query.BowlerId)
            .Select(bowler => new
            {
                bowler.Name,
                HallOfFame = bowler.HallOfFameInductions.Any(),
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

        // will need to union with stat tournaments to get winners

        return new BowlerTitlesDto
        {
            BowlerName = bowler.Name,
            HallOfFame = bowler.HallOfFame,
            Titles = [.. historicalTitles.Select(title => new BowlerTitleDto
            {
                TournamentId = title.TournamentId,
                TournamentName = title.TournamentName,
                TournamentDate = title.TournamentDate,
                TournamentType = title.TournamentType.Name
            })]
        };
    }
}