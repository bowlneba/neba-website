using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Series.CreateSeries;

namespace Neba.Api.Features.Series.CreateSeries;

internal sealed class CreateSeriesRequestValidator
    : Validator<CreateSeriesRequest>
{
    public CreateSeriesRequestValidator()
    {
        RuleFor(r => r.Series.Name)
            .NotEmpty().WithErrorCode("CreateSeriesRequest.NameRequired").WithMessage("Series name is required.")
            .MaximumLength(127).WithErrorCode("CreateSeriesRequest.NameTooLong").WithMessage("Series name must be 127 characters or fewer.");

        RuleFor(r => r.Series.SeasonId)
            .NotEmpty().Length(26).WithErrorCode("CreateSeriesRequest.SeasonIdInvalid").WithMessage("Season is required.");

        RuleForEach(r => r.Series.Stages).SetValidator(new SeriesStageInputValidator());

        RuleFor(r => r.Series.Finale!).SetValidator(new SeriesFinaleInputValidator())
            .When(r => r.Series.Finale is not null);
    }
}
