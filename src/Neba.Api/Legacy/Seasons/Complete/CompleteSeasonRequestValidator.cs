using FluentValidation;

namespace Neba.Api.Legacy.Seasons.Complete;

internal sealed class CompleteSeasonRequestValidator
    : AbstractValidator<CompleteSeasonRequest>
{
    public CompleteSeasonRequestValidator()
    {
        RuleFor(request => request.SeasonId)
            .GreaterThan(0);
    }
}