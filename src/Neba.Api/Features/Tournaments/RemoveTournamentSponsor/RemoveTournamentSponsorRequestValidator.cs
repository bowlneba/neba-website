using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.Tournaments.RemoveTournamentSponsor;

internal sealed class RemoveTournamentSponsorRequestValidator
    : Validator<RemoveTournamentSponsorRequest>
{
    public RemoveTournamentSponsorRequestValidator()
    {
        RuleFor(r => r.TournamentId)
            .NotEmpty().WithErrorCode("RemoveTournamentSponsorRequest.TournamentIdRequired").WithMessage("Tournament ID is required.")
            .Length(26).WithErrorCode("RemoveTournamentSponsorRequest.TournamentIdInvalidLength").WithMessage("Tournament ID must be a 26-character ULID.");

        RuleFor(r => r.SponsorId)
            .NotEmpty().WithErrorCode("RemoveTournamentSponsorRequest.SponsorIdRequired").WithMessage("Sponsor ID is required.")
            .Length(26).WithErrorCode("RemoveTournamentSponsorRequest.SponsorIdInvalidLength").WithMessage("Sponsor ID must be a 26-character ULID.");
    }
}
