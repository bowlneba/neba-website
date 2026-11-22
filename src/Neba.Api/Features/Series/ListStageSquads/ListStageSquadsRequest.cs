using FastEndpoints;

namespace Neba.Api.Features.Series.ListStageSquads;

internal sealed class ListStageSquadsRequest
{
    [BindFrom("id")]
    public required string SeriesId { get; set; }

    [BindFrom("stageId")]
    public required string StageId { get; set; }
}