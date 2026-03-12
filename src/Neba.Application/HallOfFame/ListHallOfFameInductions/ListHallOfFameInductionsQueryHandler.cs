using Neba.Application.Messaging;
using Neba.Application.Storage;

namespace Neba.Application.HallOfFame.ListHallOfFameInductions;

internal sealed class ListHallOfFameInductionsQueryHandler(
    IHallOfFameQueries hallOfFameQueries,
    IFileStorageService fileStorageService)
        : IQueryHandler<ListHallOfFameInductionsQuery, IReadOnlyCollection<HallOfFameInductionDto>>
{
    private readonly IHallOfFameQueries _hallOfFameQueries = hallOfFameQueries;
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<IReadOnlyCollection<HallOfFameInductionDto>> HandleAsync(ListHallOfFameInductionsQuery query, CancellationToken cancellationToken)
    {
        var inductions = await _hallOfFameQueries.GetAllAsync(cancellationToken);

        foreach (var induction in inductions.Where(i => i.PhotoContainer is not null))
        {
            induction.PhotoUri = _fileStorageService.GetBlobUri(induction.PhotoContainer!, induction.PhotoPath!);
        }

        return inductions;
    }
}