using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Sponsors.CreateSponsor;
using Neba.Api.Features.Sponsors.Domain;

namespace Neba.Api.Features.Sponsors.CreateSponsor;

internal sealed class CreateSponsorRequestValidator
    : Validator<CreateSponsorRequest>
{
    public CreateSponsorRequestValidator()
    {
        RuleFor(r => r.Sponsor.Name)
            .NotEmpty()
            .WithErrorCode("CreateSponsorRequest.NameRequired")
            .WithMessage("Name is required.")
            .MaximumLength(63)
            .WithErrorCode("CreateSponsorRequest.NameTooLong")
            .WithMessage("Name must be 63 characters or fewer.");

        RuleFor(r => r.Sponsor.Slug)
            .MaximumLength(63)
            .WithErrorCode("CreateSponsorRequest.SlugTooLong")
            .WithMessage("Slug must be 63 characters or fewer.")
            .When(r => !string.IsNullOrWhiteSpace(r.Sponsor.Slug));

        RuleFor(r => r.Sponsor.Tier)
            .NotEmpty()
            .WithErrorCode("CreateSponsorRequest.TierRequired")
            .WithMessage("Tier is required.")
            .Must(tier => SponsorTier.List.Any(t => t.Name == tier))
            .WithErrorCode("CreateSponsorRequest.TierInvalid")
            .WithMessage("Tier must be one of: Title Sponsor, Premier, Standard.");

        RuleFor(r => r.Sponsor.Category)
            .NotEmpty()
            .WithErrorCode("CreateSponsorRequest.CategoryRequired")
            .WithMessage("Category is required.")
            .Must(category => SponsorCategory.List.Any(c => c.Name == category))
            .WithErrorCode("CreateSponsorRequest.CategoryInvalid")
            .WithMessage("Category must be a known sponsor category.");

        RuleFor(r => r.Sponsor.WebsiteUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode("CreateSponsorRequest.WebsiteUrlInvalid")
            .WithMessage("WebsiteUrl must be an absolute URI.")
            .When(r => r.Sponsor.WebsiteUrl is not null);

        RuleFor(r => r.Sponsor.FacebookUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode("CreateSponsorRequest.FacebookUrlInvalid")
            .WithMessage("FacebookUrl must be an absolute URI.")
            .When(r => r.Sponsor.FacebookUrl is not null);

        RuleFor(r => r.Sponsor.InstagramUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode("CreateSponsorRequest.InstagramUrlInvalid")
            .WithMessage("InstagramUrl must be an absolute URI.")
            .When(r => r.Sponsor.InstagramUrl is not null);

        // Structural-only: all-or-nothing shape of the contact block. Whether the phone/email
        // values themselves are *valid* NANP/RFC formats is a business rule left to
        // PhoneNumber.CreateNorthAmerican / EmailAddress.Create in the handler.
        RuleFor(r => r.Sponsor.Contact)
            .Must(contact => !string.IsNullOrWhiteSpace(contact!.Name)
                && !string.IsNullOrWhiteSpace(contact.PhoneNumber)
                && !string.IsNullOrWhiteSpace(contact.Email))
            .WithErrorCode("CreateSponsorRequest.ContactIncomplete")
            .WithMessage("If any contact field is supplied, Name, PhoneNumber, and Email are all required.")
            .When(r => r.Sponsor.Contact is not null);
    }
}