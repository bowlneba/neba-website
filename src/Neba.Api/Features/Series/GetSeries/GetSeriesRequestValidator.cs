using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.Series.GetSeries;

internal sealed class GetSeriesRequestValidator : Validator<GetSeriesRequest>
{
    public GetSeriesRequestValidator()
    {
        RuleFor(r => r.SeriesId)
            .NotEmpty()
            .WithErrorCode("GetSeriesRequest.SeriesIdRequired")
            .WithMessage("Series ID is required.")
            .Length(26)
            .WithErrorCode("GetSeriesRequest.SeriesIdInvalidLength")
            .WithMessage("Series ID must be a 26-character ULID.");
    }
}