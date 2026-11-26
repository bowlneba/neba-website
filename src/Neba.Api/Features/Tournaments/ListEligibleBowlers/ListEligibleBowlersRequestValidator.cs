using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

internal sealed class ListEligibleBowlersRequestValidator : Validator<ListEligibleBowlersRequest>
{
    public ListEligibleBowlersRequestValidator()
    {
        RuleFor(r => r.TournamentId)
            .NotEmpty()
            .WithErrorCode("ListEligibleBowlersRequest.TournamentIdRequired")
            .WithMessage("Tournament ID is required.")
            .Length(26)
            .WithErrorCode("ListEligibleBowlersRequest.TournamentIdInvalidLength")
            .WithMessage("Tournament ID must be a 26-character ULID.");
    }
}