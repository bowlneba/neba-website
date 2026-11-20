using FastEndpoints;

namespace Neba.Api.Features.Series.ListStageResults;

internal sealed class ListStageResultsRequestValidator : Validator<ListStageResultsRequest>
{
    public ListStageResultsRequestValidator()
    {
        RuleFor(r => r.SeriesId).IsUlid("Series ID", "ListStageResultsRequest.SeriesId");
        RuleFor(r => r.StageId).IsUlid("Stage ID", "ListStageResultsRequest.StageId");
    }
}