using Microsoft.EntityFrameworkCore;

using Neba.Application.Awards;
using Neba.Domain.Seasons;

namespace Neba.Infrastructure.Database.Queries;

internal sealed class AwardQueries(AppDbContext dbContext)
    : IAwardQueries
{
    private readonly IQueryable<Season> _seasons
        = dbContext.Seasons.AsNoTracking();

    public async Task<IReadOnlyCollection<HighBlockAwardDto>> GetAllHighBlockAwardsAsync(CancellationToken cancellationToken)
        => await (from season in _seasons
                  from highBlockAward in season.HighBlockAwards
                  select new HighBlockAwardDto
                  {
                      Season = season.Description,
                      BowlerName = highBlockAward.Bowler.Name,
                      Score = highBlockAward.BlockScore
                  }).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<HighAverageAwardDto>> GetAllHighAverageAwardsAsync(CancellationToken cancellationToken)
        => await (from season in _seasons
                  from highAverageAward in season.HighAverageAwards
                  select new HighAverageAwardDto
                  {
                      Season = season.Description,
                      BowlerName = highAverageAward.Bowler.Name,
                      Average = highAverageAward.Average,
                      TotalGames = highAverageAward.TotalGames,
                      TournamentsParticipated = highAverageAward.TournamentsParticipated
                  }).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<BowlerOfTheYearAwardDto>> GetAllBowlerOfTheYearAwardsAsync(CancellationToken cancellationToken)
        => await (from season in _seasons
                  from award in season.BowlerOfTheYearAwards
                  select new BowlerOfTheYearAwardDto
                  {
                      Season = season.Description,
                      BowlerName = award.Bowler.Name,
                      Category = award.Category.Name
                  }).ToListAsync(cancellationToken);
}