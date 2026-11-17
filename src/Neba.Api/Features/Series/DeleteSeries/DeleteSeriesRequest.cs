using FastEndpoints;

namespace Neba.Api.Features.Series.DeleteSeries;

internal sealed class DeleteSeriesRequest
{
    [BindFrom("id")]
    public required string Id { get; set; }
}
