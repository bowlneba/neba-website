using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Tournaments.EditTournamentSponsorAmount;
using Neba.Api.Features.Tournaments.Domain;

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

        RuleFor(r => r.Sponsor)
            .NotNull().WithErrorCode("EditTournamentSponsorAmountRequest.SponsorRequired").WithMessage("Sponsor is required.");

        RuleFor(r => r.Sponsor.SponsorshipAmount)
            .InclusiveBetween(0m, TournamentSponsor.MaxSponsorshipAmount)
            .WithErrorCode("EditTournamentSponsorAmountRequest.SponsorshipAmountInvalid")
            .WithMessage("Sponsorship amount must be between $0 and $99,999.99.")
            .PrecisionScale(7, 2, ignoreTrailingZeros: true)
            .WithErrorCode("EditTournamentSponsorAmountRequest.SponsorshipAmountInvalid")
            .WithMessage("Sponsorship amount can have at most two decimal places.")
            .When(r => r.Sponsor is not null);
    }
}