using Microsoft.EntityFrameworkCore;

using Neba.Application.Bowlers;
using Neba.Domain.Bowlers;

namespace Neba.Infrastructure.Database.Queries;

internal sealed class BowlerQueries(AppDbContext appDbContext)
    : IBowlerQueries
{
    private readonly IQueryable<Bowler> _bowlers
        = appDbContext.Bowlers.AsNoTracking();

    public async Task<IReadOnlyDictionary<int, BowlerId>> GetBowlerIdByLegacyIdAsync(CancellationToken cancellationToken)
        => await _bowlers
            .Where(b => b.LegacyId.HasValue)
            .ToDictionaryAsync(b => b.LegacyId!.Value, b => b.Id, cancellationToken);
}