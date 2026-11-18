using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Series.EditSeries;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

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
            .MaximumLength(SeriesAggregate.NameMaxLength).WithErrorCode("EditSeriesRequest.NameTooLong").WithMessage($"Series name must be {SeriesAggregate.NameMaxLength} characters or fewer.");

        RuleForEach(r => r.Series.Stages).SetValidator(new SeriesStageInputValidator());

        RuleFor(r => r.Series.Finale!).SetValidator(new SeriesFinaleInputValidator())
            .When(r => r.Series.Finale is not null);
    }
}