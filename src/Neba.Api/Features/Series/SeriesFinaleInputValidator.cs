using FluentValidation;

using Neba.Api.Contracts.Series.CreateSeries;

namespace Neba.Api.Features.Series;

internal sealed class SeriesFinaleInputValidator
    : AbstractValidator<SeriesFinaleInput>
{
    public SeriesFinaleInputValidator()
    {
        RuleFor(f => f.Date)
            .NotNull()
            .WithErrorCode("SeriesFinaleInput.DateRequired")
            .WithMessage("The Finale needs a date.");

        RuleFor(f => f.StartTime)
            .NotNull()
            .WithErrorCode("SeriesFinaleInput.StartTimeRequired")
            .WithMessage("The Finale needs a start time.");

        RuleFor(f => f.BowlingCenterId)
            .MaximumLength(6)
            .When(f => !string.IsNullOrWhiteSpace(f.BowlingCenterId))
            .WithErrorCode("SeriesFinaleInput.BowlingCenterIdInvalid")
            .WithMessage("Bowling center must be a certification number.");
    }
}
