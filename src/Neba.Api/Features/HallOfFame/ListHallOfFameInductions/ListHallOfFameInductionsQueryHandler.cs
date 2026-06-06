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
        var rows = await _hallOfFameInductions
            .Select(induction => new
            {
                induction.Year,
                BowlerName = induction.Bowler.Name,
                induction.Categories,
                PhotoContainer = induction.Photo != null ? induction.Photo.Container : null,
                PhotoPath = induction.Photo != null ? induction.Photo.Path : null
            })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new HallOfFameInductionDto
        {
            Year = row.Year,
            BowlerName = row.BowlerName,
            Categories = row.Categories,
            PhotoUri = row.PhotoContainer != null && row.PhotoPath != null
                ? _fileStorageService.GetBlobUri(row.PhotoContainer, row.PhotoPath)
                : null
        })];
    }
}