using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Messaging;
using Neba.Domain.Seasons;

namespace Neba.Api.Features.Awards.ListHighBlockAwards;

internal sealed class ListHighBlockAwardsQueryHandler(AppDbContext appDbContext)
        : IQueryHandler<ListHighBlockAwardsQuery, IReadOnlyCollection<HighBlockAwardDto>>
{
    private readonly IQueryable<Season> _seasons = appDbContext.Seasons.AsNoTracking();

    public async Task<IReadOnlyCollection<HighBlockAwardDto>> HandleAsync(ListHighBlockAwardsQuery query, CancellationToken cancellationToken)
        => await (from season in _seasons
                 from highBlockAward in season.HighBlockAwards
                 select new HighBlockAwardDto
                 {
                     Season = season.Description,
                     BowlerName = highBlockAward.Bowler.Name,
                     Score = highBlockAward.BlockScore
                 }).ToListAsync(cancellationToken);
}