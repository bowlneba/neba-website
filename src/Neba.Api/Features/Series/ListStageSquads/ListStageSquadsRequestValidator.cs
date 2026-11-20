using FastEndpoints;

namespace Neba.Api.Features.Series.ListStageSquads;

internal sealed class ListStageSquadsRequestValidator : Validator<ListStageSquadsRequest>
{
    public ListStageSquadsRequestValidator()
    {
        RuleFor(r => r.SeriesId).IsUlid("Series ID", "ListStageSquadsRequest.SeriesId");
        RuleFor(r => r.StageId).IsUlid("Stage ID", "ListStageSquadsRequest.StageId");
    }
}