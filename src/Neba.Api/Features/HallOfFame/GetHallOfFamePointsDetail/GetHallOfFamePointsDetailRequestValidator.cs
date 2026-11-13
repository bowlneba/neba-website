using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.HallOfFame.GetHallOfFamePointsDetail;

internal sealed class GetHallOfFamePointsDetailRequestValidator : Validator<GetHallOfFamePointsDetailRequest>
{
    public GetHallOfFamePointsDetailRequestValidator()
    {
        RuleFor(r => r.BowlerId)
            .NotEmpty()
            .WithErrorCode("GetHallOfFamePointsDetailRequest.BowlerIdRequired")
            .WithMessage("Bowler ID is required.")
            .Length(26)
            .WithErrorCode("GetHallOfFamePointsDetailRequest.BowlerIdInvalidLength")
            .WithMessage("Bowler ID must be a 26-character ULID.");
    }
}
