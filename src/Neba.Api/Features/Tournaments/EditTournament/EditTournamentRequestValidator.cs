using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Tournaments.EditTournament;

namespace Neba.Api.Features.Tournaments.EditTournament;

internal sealed class EditTournamentRequestValidator
    : Validator<EditTournamentRequest>
{
    public EditTournamentRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty().WithErrorCode("EditTournamentRequest.IdRequired").WithMessage("Id is required.")
            .Length(26).WithErrorCode("EditTournamentRequest.IdInvalidLength").WithMessage("Id must be a 26-character ULID.");

        RuleFor(r => r.Tournament).SetValidator(new TournamentInputValidator("EditTournamentRequest"));
    }
}