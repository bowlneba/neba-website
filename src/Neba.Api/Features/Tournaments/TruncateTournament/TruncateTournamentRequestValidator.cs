using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.Tournaments.TruncateTournament;

internal sealed class TruncateTournamentRequestValidator
    : Validator<TruncateTournamentRequest>
{
    public TruncateTournamentRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty()
            .WithErrorCode("TruncateTournamentRequest.IdRequired")
            .WithMessage("Id is required.")
            .Length(26)
            .WithErrorCode("TruncateTournamentRequest.IdInvalidLength")
            .WithMessage("Id must be a 26-character ULID.");
    }
}