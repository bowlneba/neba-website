using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Sponsors.EditSponsor;
using Neba.Api.Features.Sponsors.Domain;

namespace Neba.Api.Features.Sponsors.EditSponsor;

internal sealed class EditSponsorRequestValidator
    : Validator<EditSponsorRequest>
{
    public EditSponsorRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty()
            .WithErrorCode("EditSponsorRequest.IdRequired")
            .WithMessage("Id is required.")
            .Length(26)
            .WithErrorCode("EditSponsorRequest.IdInvalidLength")
            .WithMessage("Id must be a 26-character ULID.");

        RuleFor(r => r.Sponsor.Name)
            .NotEmpty()
            .WithErrorCode("EditSponsorRequest.NameRequired")
            .WithMessage("Name is required.")
            .MaximumLength(63)
            .WithErrorCode("EditSponsorRequest.NameTooLong")
            .WithMessage("Name must be 63 characters or fewer.");

        RuleFor(r => r.Sponsor.Tier)
            .NotEmpty()
            .WithErrorCode("EditSponsorRequest.TierRequired")
            .WithMessage("Tier is required.")
            .Must(tier => SponsorTier.List.Any(t => t.Name == tier))
            .WithErrorCode("EditSponsorRequest.TierInvalid")
            .WithMessage("Tier must be one of: Title Sponsor, Premier, Standard.");

        RuleFor(r => r.Sponsor.Category)
            .NotEmpty()
            .WithErrorCode("EditSponsorRequest.CategoryRequired")
            .WithMessage("Category is required.")
            .Must(category => SponsorCategory.List.Any(c => c.Name == category))
            .WithErrorCode("EditSponsorRequest.CategoryInvalid")
            .WithMessage("Category must be a known sponsor category.");

        RuleFor(r => r.Sponsor.WebsiteUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode("EditSponsorRequest.WebsiteUrlInvalid")
            .WithMessage("WebsiteUrl must be an absolute URI.")
            .When(r => r.Sponsor.WebsiteUrl is not null);

        RuleFor(r => r.Sponsor.FacebookUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode("EditSponsorRequest.FacebookUrlInvalid")
            .WithMessage("FacebookUrl must be an absolute URI.")
            .When(r => r.Sponsor.FacebookUrl is not null);

        RuleFor(r => r.Sponsor.InstagramUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode("EditSponsorRequest.InstagramUrlInvalid")
            .WithMessage("InstagramUrl must be an absolute URI.")
            .When(r => r.Sponsor.InstagramUrl is not null);

        // Structural-only: all-or-nothing shape of the contact block. Whether the phone/email
        // values themselves are *valid* NANP/RFC formats is a business rule left to
        // PhoneNumber.CreateNorthAmerican / EmailAddress.Create in the handler.
        RuleFor(r => r.Sponsor.Contact)
            .Must(contact => !string.IsNullOrWhiteSpace(contact!.Name)
                && !string.IsNullOrWhiteSpace(contact.PhoneNumber)
                && !string.IsNullOrWhiteSpace(contact.Email))
            .WithErrorCode("EditSponsorRequest.ContactIncomplete")
            .WithMessage("If any contact field is supplied, Name, PhoneNumber, and Email are all required.")
            .When(r => r.Sponsor.Contact is not null);
    }
}
