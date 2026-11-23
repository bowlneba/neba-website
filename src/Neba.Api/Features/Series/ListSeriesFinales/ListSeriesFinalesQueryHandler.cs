using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Messaging;
using Neba.Api.Storage;

namespace Neba.Api.Features.Series.ListSeriesFinales;

internal sealed class ListSeriesFinalesQueryHandler(AppDbContext appDbContext, IFileStorageService fileStorageService)
    : IQueryHandler<ListSeriesFinalesQuery, IReadOnlyCollection<SeriesFinaleSummaryDto>>
{
    public async Task<IReadOnlyCollection<SeriesFinaleSummaryDto>> HandleAsync(ListSeriesFinalesQuery query, CancellationToken cancellationToken)
    {
        var rows = await appDbContext.Series.AsNoTracking()
            .Where(series => series.Finale != null)
            .Select(series => new
            {
                series.Id,
                series.Name,
                series.SeasonId,
                series.Finale,
                LogoContainer = series.Logo == null ? null : series.Logo.Container,
                LogoPath = series.Logo == null ? null : series.Logo.Path,
            })
            .ToListAsync(cancellationToken);

        var centerIds = rows.Select(row => row.Finale!.BowlingCenterId).OfType<CertificationNumber>().Distinct().ToList();

        var centers = await appDbContext.BowlingCenters.AsNoTracking()
            .Where(center => centerIds.Contains(center.CertificationNumber))
            .Select(center => new { center.CertificationNumber, center.Name, center.Address.City, State = center.Address.Region })
            .ToDictionaryAsync(center => center.CertificationNumber, cancellationToken);

        return [.. rows
            .OrderBy(row => row.Finale!.Date)
            .ThenBy(row => row.Finale!.StartTime)
            .Select(row =>
            {
                var finale = row.Finale!;
                var center = finale.BowlingCenterId is { } id ? centers.GetValueOrDefault(id) : null;

                return new SeriesFinaleSummaryDto
                {
                    SeriesId = row.Id,
                    SeriesName = row.Name,
                    SeasonId = row.SeasonId,
                    LogoUrl = row.LogoContainer is not null && row.LogoPath is not null
                        ? fileStorageService.GetBlobUri(row.LogoContainer, row.LogoPath)
                        : null,
                    Finale = new SeriesFinaleScheduleDto
                    {
                        FinalistCount = finale.FinalistCount,
                        Date = finale.Date,
                        StartTime = finale.StartTime,
                        BowlingCenterId = finale.BowlingCenterId?.Value,
                        BowlingCenterName = center?.Name,
                        BowlingCenterCity = center?.City,
                        BowlingCenterState = center?.State,
                    },
                };
            })];
    }
}