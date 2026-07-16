using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Contacts;
using Neba.Api.Contacts.Domain;
using Neba.Api.Database;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Sponsors.CreateSponsor;

internal sealed class CreateSponsorCommandHandler(
        AppDbContext appDbContext,
        IFusionCache cache)
    : ICommandHandler<CreateSponsorCommand, CreatedSponsor>
{
    public async Task<ErrorOr<CreatedSponsor>> HandleAsync(CreateSponsorCommand command, CancellationToken cancellationToken)
    {
        var addressResult = BuildBusinessAddress(command);

        if (addressResult.IsError)
        {
            return addressResult.Errors;
        }

        var emailResult = BuildBusinessEmail(command.BusinessEmailAddress);

        if (emailResult.IsError)
        {
            return emailResult.Errors;
        }

        var phoneNumbersResult = BuildPhoneNumbers(command.PhoneNumbers);

        if (phoneNumbersResult.IsError)
        {
            return phoneNumbersResult.Errors;
        }

        var contactResult = BuildSponsorContact(command);

        if (contactResult.IsError)
        {
            return contactResult.Errors;
        }

        var titleSponsorshipTaken = command.Tier == SponsorTier.TitleSponsor
            && await appDbContext.Sponsors.AnyAsync(sponsor => sponsor.IsCurrentSponsor && sponsor.Tier == SponsorTier.TitleSponsor, cancellationToken);

        var sponsorResult = Sponsor.Create(
            name: command.Name,
            isCurrentSponsor: command.IsCurrentSponsor,
            priority: command.Priority,
            tier: command.Tier,
            category: command.Category,
            isTitleSponsorshipAvailable: !titleSponsorshipTaken,
            slug: command.Slug,
            logo: command.Logo,
            websiteUrl: command.WebsiteUrl,
            tagPhrase: command.TagPhrase,
            description: command.Description,
            liveReadText: command.LiveReadText,
            promotionalNotes: command.PromotionalNotes,
            facebookUrl: command.FacebookUrl,
            instagramUrl: command.InstagramUrl,
            businessAddress: addressResult.Value,
            businessEmail: emailResult.Value,
            phoneNumbers: phoneNumbersResult.Value,
            sponsorContact: contactResult.Value
        );

        if (sponsorResult.IsError)
        {
            return sponsorResult.Errors;
        }

        var sponsor = sponsorResult.Value;

        var slugCheck = await EnsureSlugIsAvailableAsync(sponsor.Slug, cancellationToken);

        if (slugCheck.IsError)
        {
            return slugCheck.Errors;
        }

        await appDbContext.Sponsors.AddAsync(sponsor, cancellationToken);
        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:sponsors", token: cancellationToken);

        return new CreatedSponsor
        {
            Id = sponsor.Id,
            Slug = sponsor.Slug
        };
    }

    private static ErrorOr<Address?> BuildBusinessAddress(CreateSponsorCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.BusinessStreet))
        {
            return (Address?)null;
        }

        ArgumentNullException.ThrowIfNull(command.BusinessState);

        var result = Address.Create(
            command.BusinessStreet,
            command.BusinessUnit,
            command.BusinessCity ?? string.Empty,
            command.BusinessState,
            command.BusinessPostalCode ?? string.Empty);

        return result.IsError
            ? result.Errors
            : result.Value;
    }

    private static ErrorOr<EmailAddress?> BuildBusinessEmail(string? businessEmailAddress)
    {
        if (string.IsNullOrWhiteSpace(businessEmailAddress))
        {
            return (EmailAddress?)null;
        }

        var result = EmailAddress.Create(businessEmailAddress);

        return result.IsError
            ? result.Errors
            : result.Value;
    }

    private static ErrorOr<IReadOnlyCollection<PhoneNumber>> BuildPhoneNumbers(
        IReadOnlyCollection<PhoneNumberInput> phoneNumbers)
    {
        var built = new List<PhoneNumber>(phoneNumbers.Count);

        foreach (var phoneNumber in phoneNumbers)
        {
            var result = PhoneNumber.CreateNorthAmerican(phoneNumber.Type, phoneNumber.Number, phoneNumber.Extension);

            if (result.IsError)
            {
                return result.Errors;
            }

            built.Add(result.Value);
        }

        return built;
    }

    // All-or-nothing per scoping decision: if any of Name/Phone/Email is supplied, all three must be.
    private static ErrorOr<ContactInfo?> BuildSponsorContact(CreateSponsorCommand command)
    {
        var anySupplied = !string.IsNullOrWhiteSpace(command.ContactName)
            || !string.IsNullOrWhiteSpace(command.ContactPhoneNumber)
            || !string.IsNullOrWhiteSpace(command.ContactEmail);

        if (!anySupplied)
        {
            return (ContactInfo?)null;
        }

        ArgumentNullException.ThrowIfNull(command.ContactPhoneType);

        var phoneResult = PhoneNumber.CreateNorthAmerican(
            command.ContactPhoneType,
            command.ContactPhoneNumber ?? string.Empty,
            command.ContactPhoneExtension);

        if (phoneResult.IsError)
        {
            return phoneResult.Errors;
        }

        var emailResult = EmailAddress.Create(command.ContactEmail ?? string.Empty);

        return emailResult.IsError
            ? emailResult.Errors
            : new ContactInfo
            {
                Name = command.ContactName ?? string.Empty,
                Phone = phoneResult.Value,
                Email = emailResult.Value
            };
    }

    // Check-then-insert: see CreateArticleCommandHandler.EnsureSlugIsAvailableAsync for the same
    // caveat about a theoretical concurrent-insert race — not worth a retry path at current volume.
    private async Task<ErrorOr<Success>> EnsureSlugIsAvailableAsync(string slug, CancellationToken cancellationToken)
    {
        var slugExists = await appDbContext.Sponsors.AnyAsync(s => s.Slug == slug, cancellationToken);

        return slugExists
            ? SponsorErrors.SlugAlreadyExists(slug)
            : Result.Success;
    }
}