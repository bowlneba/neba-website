using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.Tournaments.DeleteTournament;

internal sealed class DeleteTournamentRequestValidator
    : Validator<DeleteTournamentRequest>
{
    public DeleteTournamentRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty()
            .WithErrorCode("DeleteTournamentRequest.IdRequired")
            .WithMessage("Id is required.")
            .Length(26)
            .WithErrorCode("DeleteTournamentRequest.IdInvalidLength")
            .WithMessage("Id must be a 26-character ULID.");
    }
}