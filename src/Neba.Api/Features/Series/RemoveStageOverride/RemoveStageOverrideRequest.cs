using FastEndpoints;

namespace Neba.Api.Features.Series.RemoveStageOverride;

internal sealed class RemoveStageOverrideRequest
{
    [BindFrom("id")]
    public required string Id { get; set; }

    [BindFrom("stageId")]
    public required string StageId { get; set; }

    [BindFrom("overrideId")]
    public required string OverrideId { get; set; }

    [BindFrom("version")]
    public int? Version { get; set; }
}