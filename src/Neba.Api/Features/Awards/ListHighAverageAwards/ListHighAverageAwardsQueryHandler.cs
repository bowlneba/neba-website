using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Messaging;
using Neba.Domain.Seasons;

namespace Neba.Api.Features.Awards.ListHighAverageAwards;

internal sealed class ListHighAverageAwardsQueryHandler(AppDbContext appDbContext)
    : IQueryHandler<ListHighAverageAwardsQuery, IReadOnlyCollection<HighAverageAwardDto>>
{
    private readonly IQueryable<Season> _seasons = appDbContext.Seasons.AsNoTracking();

    public async Task<IReadOnlyCollection<HighAverageAwardDto>> HandleAsync(
        ListHighAverageAwardsQuery query, CancellationToken cancellationToken)
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
}