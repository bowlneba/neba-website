using FastEndpoints;

namespace Neba.Api.Features.Series.GetSeriesAdvancement;

internal sealed class GetSeriesAdvancementRequest
{
    [BindFrom("id")]
    public required string SeriesId { get; set; }
}