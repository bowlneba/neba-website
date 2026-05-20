using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.Tournaments.ListTournamentsInSeason;

internal sealed class ListTournamentsInSeasonRequestValidator
    : Validator<ListTournamentsInSeasonRequest>
{
    public ListTournamentsInSeasonRequestValidator()
    {
        RuleFor(r => r.SeasonId)
            .NotEmpty()
            .WithErrorCode("ListTournamentsInSeasonRequest.SeasonIdRequired")
            .WithMessage("Season ID is required.")
            .Length(26)
            .WithErrorCode("ListTournamentsInSeasonRequest.SeasonIdInvalidLength")
            .WithMessage("Season ID must be a 26-character ULID.");
    }
}