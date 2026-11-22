using FastEndpoints;

namespace Neba.Api.Features.Series.GetSeriesAdvancement;

internal sealed class GetSeriesAdvancementRequestValidator : Validator<GetSeriesAdvancementRequest>
{
    public GetSeriesAdvancementRequestValidator()
        => RuleFor(r => r.SeriesId).IsUlid("Series ID", "GetSeriesAdvancementRequest.SeriesId");
}