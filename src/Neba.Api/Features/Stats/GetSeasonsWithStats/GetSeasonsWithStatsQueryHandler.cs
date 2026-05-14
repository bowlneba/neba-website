using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Messaging;
using Neba.Domain.Stats;

namespace Neba.Api.Features.Stats.GetSeasonsWithStats;

internal sealed class GetSeasonsWithStatsQueryHandler(AppDbContext appDbContext)
    : IQueryHandler<GetSeasonsWithStatsQuery, IReadOnlyCollection<SeasonWithStatsDto>>
{
    private readonly IQueryable<BowlerSeasonStats> _bowlerSeasonStats
        = appDbContext.BowlerSeasonStats.AsNoTracking();

    public async Task<IReadOnlyCollection<SeasonWithStatsDto>> HandleAsync(GetSeasonsWithStatsQuery query, CancellationToken cancellationToken)
        => await _bowlerSeasonStats
            .Select(stat => new SeasonWithStatsDto
            {
                Id = stat.Season.Id,
                Description = stat.Season.Description,
                StartDate = stat.Season.StartDate,
                EndDate = stat.Season.EndDate
            }).ToListAsync(cancellationToken);
}
