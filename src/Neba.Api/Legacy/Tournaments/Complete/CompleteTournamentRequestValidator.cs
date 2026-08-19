using FluentValidation;

namespace Neba.Api.Legacy.Tournaments.Complete;

internal sealed class CompleteTournamentRequestValidator
    : AbstractValidator<CompleteTournamentRequest>
{
    public CompleteTournamentRequestValidator()
    {
        RuleFor(request => request.TournamentId)
            .GreaterThan(0);
    }
}