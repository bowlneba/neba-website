using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Messaging;
using Neba.Api.Storage;

namespace Neba.Api.Features.HallOfFame.ListHallOfFameInductions;

internal sealed class ListHallOfFameInductionsQueryHandler(
    AppDbContext appDbContext,
    IFileStorageService fileStorageService)
        : IQueryHandler<ListHallOfFameInductionsQuery, IReadOnlyCollection<HallOfFameInductionDto>>
{
    private readonly IQueryable<HallOfFameInduction> _hallOfFameInductions = appDbContext.HallOfFameInductions.AsNoTracking();
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<IReadOnlyCollection<HallOfFameInductionDto>> HandleAsync(ListHallOfFameInductionsQuery query, CancellationToken cancellationToken)
    {
        var inductions = await _hallOfFameInductions
            .Select(induction => new HallOfFameInductionDto
            {
                Year = induction.Year,
                BowlerName = induction.Bowler.Name,
                Categories = induction.Categories,
                PhotoContainer = induction.Photo != null ? induction.Photo.Container : null,
                PhotoPath = induction.Photo != null ? induction.Photo.Path : null
            })
            .ToListAsync(cancellationToken);

        foreach (var induction in inductions.Where(i => i.PhotoContainer is not null && i.PhotoPath is not null))
        {
            induction.PhotoUri = _fileStorageService.GetBlobUri(induction.PhotoContainer!, induction.PhotoPath!);
        }

        return inductions;
    }
}