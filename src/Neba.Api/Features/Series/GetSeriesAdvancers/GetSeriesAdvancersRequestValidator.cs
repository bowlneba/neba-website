using FastEndpoints;

namespace Neba.Api.Features.Series.GetSeriesAdvancers;

internal sealed class GetSeriesAdvancersRequestValidator : Validator<GetSeriesAdvancersRequest>
{
    public GetSeriesAdvancersRequestValidator()
        => RuleFor(r => r.SeriesId).IsUlid("Series ID", "GetSeriesAdvancersRequest.SeriesId");
}
