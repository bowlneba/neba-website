using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Tournaments.AddTournamentSponsor;

namespace Neba.Api.Features.Tournaments.AddTournamentSponsor;

internal sealed class AddTournamentSponsorRequestValidator
    : Validator<AddTournamentSponsorRequest>
{
    public AddTournamentSponsorRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty().WithErrorCode("AddTournamentSponsorRequest.IdRequired").WithMessage("Tournament ID is required.")
            .Length(26).WithErrorCode("AddTournamentSponsorRequest.IdInvalidLength").WithMessage("Tournament ID must be a 26-character ULID.");

        RuleFor(r => r.Sponsor.SponsorId)
            .NotEmpty().WithErrorCode("AddTournamentSponsorRequest.SponsorIdRequired").WithMessage("Sponsor ID is required.")
            .Length(26).WithErrorCode("AddTournamentSponsorRequest.SponsorIdInvalidLength").WithMessage("Sponsor ID must be a 26-character ULID.");

        RuleFor(r => r.Sponsor.SponsorshipAmount)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("AddTournamentSponsorRequest.SponsorshipAmountInvalid")
            .WithMessage("Sponsorship amount must be zero or greater.");
    }
}