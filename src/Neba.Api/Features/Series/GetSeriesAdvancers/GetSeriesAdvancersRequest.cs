using FastEndpoints;

namespace Neba.Api.Features.Series.GetSeriesAdvancers;

internal sealed class GetSeriesAdvancersRequest
{
    [BindFrom("id")]
    public required string SeriesId { get; set; }
}