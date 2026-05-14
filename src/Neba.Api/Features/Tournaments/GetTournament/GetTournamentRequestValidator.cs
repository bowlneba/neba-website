using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.Tournaments.GetTournament;

internal sealed class GetTournamentRequestValidator : Validator<GetTournamentRequest>
{
    public GetTournamentRequestValidator()
    {
        RuleFor(r => r.TournamentId)
            .NotEmpty()
            .WithErrorCode("GetTournamentRequest.TournamentIdRequired")
            .WithMessage("Tournament ID is required.")
            .Length(26)
            .WithErrorCode("GetTournamentRequest.TournamentIdInvalidLength")
            .WithMessage("Tournament ID must be a 26-character ULID.");
    }
}