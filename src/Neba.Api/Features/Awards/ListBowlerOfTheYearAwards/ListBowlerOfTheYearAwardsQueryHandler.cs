using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Awards.ListBowlerOfTheYearAwards;

internal sealed class ListBowlerOfTheYearAwardsQueryHandler(AppDbContext appDbContext)
    : IQueryHandler<ListBowlerOfTheYearAwardsQuery, IReadOnlyCollection<BowlerOfTheYearAwardDto>>
{
    private readonly IQueryable<Season> _seasons = appDbContext.Seasons.AsNoTracking();

    public async Task<IReadOnlyCollection<BowlerOfTheYearAwardDto>> HandleAsync(
        ListBowlerOfTheYearAwardsQuery query, CancellationToken cancellationToken)
        => await (from season in _seasons
                  from award in season.BowlerOfTheYearAwards
                  select new BowlerOfTheYearAwardDto
                  {
                      Season = season.Description,
                      BowlerName = award.Bowler.Name,
                      Category = award.Category.Name
                  }).ToListAsync(cancellationToken);
}