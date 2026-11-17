using FastEndpoints;

namespace Neba.Api.Features.Series.GetSeries;

internal sealed class GetSeriesRequest
{
    [BindFrom("id")]
    public required string SeriesId { get; set; }
}
