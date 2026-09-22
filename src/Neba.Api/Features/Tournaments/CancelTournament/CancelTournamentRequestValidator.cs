using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.Tournaments.CancelTournament;

internal sealed class CancelTournamentRequestValidator
    : Validator<CancelTournamentRequest>
{
    public CancelTournamentRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty()
            .WithErrorCode("CancelTournamentRequest.IdRequired")
            .WithMessage("Id is required.")
            .Length(26)
            .WithErrorCode("CancelTournamentRequest.IdInvalidLength")
            .WithMessage("Id must be a 26-character ULID.");
    }
}
