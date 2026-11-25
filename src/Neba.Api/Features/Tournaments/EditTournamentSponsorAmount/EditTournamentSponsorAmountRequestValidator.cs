using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Tournaments.EditTournamentSponsorAmount;

namespace Neba.Api.Features.Tournaments.EditTournamentSponsorAmount;

internal sealed class EditTournamentSponsorAmountRequestValidator
    : Validator<EditTournamentSponsorAmountRequest>
{
    public EditTournamentSponsorAmountRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty().WithErrorCode("EditTournamentSponsorAmountRequest.IdRequired").WithMessage("Tournament ID is required.")
            .Length(26).WithErrorCode("EditTournamentSponsorAmountRequest.IdInvalidLength").WithMessage("Tournament ID must be a 26-character ULID.");

        RuleFor(r => r.SponsorId)
            .NotEmpty().WithErrorCode("EditTournamentSponsorAmountRequest.SponsorIdRequired").WithMessage("Sponsor ID is required.")
            .Length(26).WithErrorCode("EditTournamentSponsorAmountRequest.SponsorIdInvalidLength").WithMessage("Sponsor ID must be a 26-character ULID.");

        RuleFor(r => r.Sponsor.SponsorshipAmount)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("EditTournamentSponsorAmountRequest.SponsorshipAmountInvalid")
            .WithMessage("Sponsorship amount must be zero or greater.");
    }
}
