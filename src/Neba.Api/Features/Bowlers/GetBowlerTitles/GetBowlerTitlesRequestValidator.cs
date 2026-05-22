using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.Bowlers.GetBowlerTitles;

internal sealed class GetBowlerTitlesRequestValidator : Validator<GetBowlerTitlesRequest>
{
    public GetBowlerTitlesRequestValidator()
    {
        RuleFor(r => r.BowlerId)
            .NotEmpty()
            .WithErrorCode("GetBowlerTitlesRequest.BowlerIdRequired")
            .WithMessage("Bowler ID is required.")
            .Length(26)
            .WithErrorCode("GetBowlerTitlesRequest.BowlerIdInvalidLength")
            .WithMessage("Bowler ID must be a 26-character ULID.");
    }
}
