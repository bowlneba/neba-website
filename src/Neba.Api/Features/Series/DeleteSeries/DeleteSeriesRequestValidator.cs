using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.Series.DeleteSeries;

internal sealed class DeleteSeriesRequestValidator : Validator<DeleteSeriesRequest>
{
    public DeleteSeriesRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty()
            .WithErrorCode("DeleteSeriesRequest.IdRequired")
            .WithMessage("Series ID is required.")
            .Length(26)
            .WithErrorCode("DeleteSeriesRequest.IdInvalidLength")
            .WithMessage("Series ID must be a 26-character ULID.");
    }
}