using FluentValidation;

using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series;

internal sealed class SeriesStageInputValidator
    : AbstractValidator<SeriesStageInput>
{
    public SeriesStageInputValidator()
    {
        RuleFor(s => s.Id)
            .Length(26).When(s => s.Id is not null)
            .WithErrorCode("SeriesStageInput.IdInvalidLength")
            .WithMessage("Stage ID must be a 26-character ULID.");

        RuleFor(s => s.Kind)
            .Must(kind => StageKind.TryFromName(kind, out _))
            .WithErrorCode("SeriesStageInput.KindInvalid")
            .WithMessage("Stage kind must be Qualifying, Survivor, or Placement.");

        RuleFor(s => s.TournamentId)
            .NotEmpty().Length(26)
            .WithErrorCode("SeriesStageInput.TournamentIdInvalid")
            .WithMessage("Each Stage needs a tournament.");

        RuleFor(s => s)
            .Must(s => (s.SquadDate is null) == (s.SquadStartTime is null))
            .WithName(nameof(SeriesStageInput.SquadDate))
            .WithErrorCode("SeriesStageInput.SquadIncomplete")
            .WithMessage("Squad date and start time go together.");
    }
}