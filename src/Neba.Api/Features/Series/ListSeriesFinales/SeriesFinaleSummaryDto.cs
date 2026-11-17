using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.GetSeries;

namespace Neba.Api.Features.Series.ListSeriesFinales;

internal sealed record SeriesFinaleSummaryDto
{
    public required SeriesId SeriesId { get; init; }
    public required string SeriesName { get; init; }
    public required SeasonId SeasonId { get; init; }
    public Uri? LogoUrl { get; init; }
    public required SeriesFinaleDto Finale { get; init; }
}
