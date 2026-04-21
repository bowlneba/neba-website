using Microsoft.EntityFrameworkCore;

using Neba.Domain.Seasons;

namespace Neba.Infrastructure.Database.Repositories;

internal sealed class SeasonRepository(AppDbContext appDbContext)
    : ISeasonRepository
{
    private readonly DbSet<Season> _seasons = appDbContext.Seasons;

    public async Task<Season?> GetSeasonByIdAsync(SeasonId id, bool trackChanges = true, CancellationToken cancellationToken = default)
    {
        IQueryable<Season> query = _seasons;

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(season => season.Id == id, cancellationToken);
    }
}