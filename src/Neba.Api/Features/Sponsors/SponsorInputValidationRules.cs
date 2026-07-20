using System.Linq.Expressions;

using FluentValidation;

using Neba.Api.Contracts.Sponsors.CreateSponsor;
using Neba.Api.Features.Sponsors.Domain;

namespace Neba.Api.Features.Sponsors;

/// <summary>
/// Structural validation rules shared by the Create and Edit sponsor request validators —
/// everything <c>SponsorInput</c> and <c>EditSponsorInput</c> have in common. Slug and Id rules
/// are request-specific and stay in each validator.
/// </summary>
internal static class SponsorInputValidationRules
{
    public static void ApplySponsorInputRules<TRequest>(
        this AbstractValidator<TRequest> validator,
        Expression<Func<TRequest, string>> name,
        Expression<Func<TRequest, string>> tier,
        Expression<Func<TRequest, string>> category,
        Expression<Func<TRequest, Uri?>> websiteUrl,
        Expression<Func<TRequest, Uri?>> facebookUrl,
        Expression<Func<TRequest, Uri?>> instagramUrl,
        Expression<Func<TRequest, SponsorContactInput?>> contact,
        string errorPrefix)
    {
        var websiteUrlGetter = websiteUrl.Compile();
        var facebookUrlGetter = facebookUrl.Compile();
        var instagramUrlGetter = instagramUrl.Compile();
        var contactGetter = contact.Compile();

        validator.RuleFor(name)
            .NotEmpty()
            .WithErrorCode($"{errorPrefix}.NameRequired")
            .WithMessage("Name is required.")
            .MaximumLength(63)
            .WithErrorCode($"{errorPrefix}.NameTooLong")
            .WithMessage("Name must be 63 characters or fewer.");

        validator.RuleFor(tier)
            .NotEmpty()
            .WithErrorCode($"{errorPrefix}.TierRequired")
            .WithMessage("Tier is required.")
            .Must(t => SponsorTier.List.Any(known => known.Name == t))
            .WithErrorCode($"{errorPrefix}.TierInvalid")
            .WithMessage("Tier must be one of: Title Sponsor, Premier, Standard.");

        validator.RuleFor(category)
            .NotEmpty()
            .WithErrorCode($"{errorPrefix}.CategoryRequired")
            .WithMessage("Category is required.")
            .Must(c => SponsorCategory.List.Any(known => known.Name == c))
            .WithErrorCode($"{errorPrefix}.CategoryInvalid")
            .WithMessage("Category must be a known sponsor category.");

        validator.RuleFor(websiteUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode($"{errorPrefix}.WebsiteUrlInvalid")
            .WithMessage("WebsiteUrl must be an absolute URI.")
            .When(r => websiteUrlGetter(r) is not null);

        validator.RuleFor(facebookUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode($"{errorPrefix}.FacebookUrlInvalid")
            .WithMessage("FacebookUrl must be an absolute URI.")
            .When(r => facebookUrlGetter(r) is not null);

        validator.RuleFor(instagramUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode($"{errorPrefix}.InstagramUrlInvalid")
            .WithMessage("InstagramUrl must be an absolute URI.")
            .When(r => instagramUrlGetter(r) is not null);

        // Structural-only: all-or-nothing shape of the contact block. Whether the phone/email
        // values themselves are *valid* NANP/RFC formats is a business rule left to
        // PhoneNumber.CreateNorthAmerican / EmailAddress.Create in the handler.
        validator.RuleFor(contact)
            .Must(c => !string.IsNullOrWhiteSpace(c!.Name)
                && !string.IsNullOrWhiteSpace(c.PhoneNumber)
                && !string.IsNullOrWhiteSpace(c.Email))
            .WithErrorCode($"{errorPrefix}.ContactIncomplete")
            .WithMessage("If any contact field is supplied, Name, PhoneNumber, and Email are all required.")
            .When(r => contactGetter(r) is not null);
    }
}