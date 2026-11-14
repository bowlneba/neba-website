using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.Tournaments.CheckTeamEligibility;

internal sealed class CheckTeamEligibilityRequestValidator : Validator<CheckTeamEligibilityRequest>
{
    // The largest team (baker) has five bowlers.
    private const int MaxTeamSize = 5;

    public CheckTeamEligibilityRequestValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty()
            .WithErrorCode("CheckTeamEligibilityRequest.IdRequired")
            .WithMessage("Tournament ID is required.")
            .Length(26)
            .WithErrorCode("CheckTeamEligibilityRequest.IdInvalidLength")
            .WithMessage("Tournament ID must be a 26-character ULID.");

        RuleForEach(request => request.BowlerIds)
            .Length(26)
            .WithErrorCode("CheckTeamEligibilityRequest.BowlerIdInvalidLength")
            .WithMessage("Bowler IDs must be 26-character ULIDs.");

        RuleFor(request => request.BowlerIds)
            .Must(ids => ids.Length <= MaxTeamSize)
            .WithErrorCode("CheckTeamEligibilityRequest.TooManyBowlerIds")
            .WithMessage("A team has at most 5 bowlers.")
            .Must(ids => ids.Distinct(StringComparer.Ordinal).Count() == ids.Length)
            .WithErrorCode("CheckTeamEligibilityRequest.BowlerIdsDuplicate")
            .WithMessage("A bowler can appear only once on a team.");

        RuleFor(request => request.UnknownBowlerCount)
            .InclusiveBetween(0, MaxTeamSize)
            .WithErrorCode("CheckTeamEligibilityRequest.UnknownBowlerCountInvalid")
            .WithMessage("Unknown bowler count must be between 0 and 5.");
    }
}