using Microsoft.EntityFrameworkCore;

using Neba.Application.Bowlers;
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
                BowlerName = new BowlerNameDto
                {
                    FirstName = induction.Bowler.Name.FirstName,
                    LastName = induction.Bowler.Name.LastName,
                    MiddleName = induction.Bowler.Name.MiddleName,
                    Suffix = induction.Bowler.Name.Suffix != null ? induction.Bowler.Name.Suffix.Value : null,
                    Nickname = induction.Bowler.Name.Nickname
                },
                Categories = induction.Categories,
                PhotoContainer = induction.Photo != null ? induction.Photo.Container : null,
                PhotoPath = induction.Photo != null ? induction.Photo.Path : null
            })
            .ToListAsync(cancellationToken);
}