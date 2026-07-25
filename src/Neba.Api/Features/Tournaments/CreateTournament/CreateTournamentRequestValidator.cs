using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Tournaments.CreateTournament;

namespace Neba.Api.Features.Tournaments.CreateTournament;

internal sealed class CreateTournamentRequestValidator
    : Validator<CreateTournamentRequest>
{
    public CreateTournamentRequestValidator()
    {
        RuleFor(r => r.Tournament).SetValidator(new TournamentInputValidator("CreateTournamentRequest"));
    }
}