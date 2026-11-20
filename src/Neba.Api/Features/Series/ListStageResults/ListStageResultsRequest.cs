using FastEndpoints;

namespace Neba.Api.Features.Series.ListStageResults;

internal sealed class ListStageResultsRequest
{
    [BindFrom("id")]
    public required string SeriesId { get; set; }

    [BindFrom("stageId")]
    public required string StageId { get; set; }
}