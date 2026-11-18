using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Series.EditSeries;

namespace Neba.Api.Features.Series.EditSeries;

internal sealed class EditSeriesRequestValidator
    : Validator<EditSeriesRequest>
{
    public EditSeriesRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty().WithErrorCode("EditSeriesRequest.IdRequired").WithMessage("Series ID is required.")
            .Length(26).WithErrorCode("EditSeriesRequest.IdInvalidLength").WithMessage("Series ID must be a 26-character ULID.");

        RuleFor(r => r.Series.Name)
            .NotEmpty().WithErrorCode("EditSeriesRequest.NameRequired").WithMessage("Series name is required.")
            .MaximumLength(127).WithErrorCode("EditSeriesRequest.NameTooLong").WithMessage("Series name must be 127 characters or fewer.");

        RuleForEach(r => r.Series.Stages).SetValidator(new SeriesStageInputValidator());

        RuleFor(r => r.Series.Finale!).SetValidator(new SeriesFinaleInputValidator())
            .When(r => r.Series.Finale is not null);
    }
}