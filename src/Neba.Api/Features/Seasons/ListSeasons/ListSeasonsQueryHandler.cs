using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Messaging;
using Neba.Domain.Seasons;

namespace Neba.Api.Features.Seasons.ListSeasons;

internal sealed class ListSeasonsQueryHandler(AppDbContext appDbContext)
        : IQueryHandler<ListSeasonsQuery, IReadOnlyCollection<SeasonDto>>
{
    private readonly IQueryable<Season> _seasons = appDbContext.Seasons.AsNoTracking();

    public async Task<IReadOnlyCollection<SeasonDto>> HandleAsync(ListSeasonsQuery query, CancellationToken cancellationToken)
        => await _seasons
            .OrderByDescending(s => s.StartDate)
            .Select(s => new SeasonDto
            {
                Id = s.Id,
                Description = s.Description,
                StartDate = s.StartDate,
                EndDate = s.EndDate
            })
            .ToListAsync(cancellationToken);
}