using Microsoft.EntityFrameworkCore;

using Neba.Application.HallOfFame;
using Neba.Application.HallOfFame.ListHallOfFameInductions;
using Neba.Domain.HallOfFame;

namespace Neba.Infrastructure.Database.Queries;

internal sealed class HallOfFameQueries(AppDbContext appDbContext)
    : IHallOfFameQueries
{
    private readonly IQueryable<HallOfFameInduction> _hallOfFameInductions
        = appDbContext.HallOfFameInductions.AsNoTracking();

    public async Task<IReadOnlyCollection<HallOfFameInductionDto>> GetAllAsync(CancellationToken cancellationToken)
        => await _hallOfFameInductions
            .Select(induction => new HallOfFameInductionDto
            {
                Year = induction.Year,
                BowlerName = induction.Bowler.Name,
                Categories = induction.Categories,
                PhotoContainer = induction.Photo != null ? induction.Photo.Container : null,
                PhotoPath = induction.Photo != null ? induction.Photo.Path : null
            })
            .ToListAsync(cancellationToken);
}