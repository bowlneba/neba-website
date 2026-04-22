using Microsoft.EntityFrameworkCore;

using Neba.Application.Seasons;
using Neba.Domain.Seasons;

namespace Neba.Infrastructure.Database.Queries;

internal sealed class SeasonQueries(AppDbContext appDbContext)
    : ISeasonQueries
{
    private readonly IQueryable<Season> _seasons
        = appDbContext.Seasons.AsNoTracking();

    public async Task<IReadOnlyCollection<SeasonDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _seasons
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
}