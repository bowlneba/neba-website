using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Storage.Domain;
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
        var addressResult = SponsorFieldBuilder.BuildBusinessAddress(
            command.BusinessStreet, command.BusinessUnit, command.BusinessCity, command.BusinessState, command.BusinessPostalCode);

        if (addressResult.IsError)
        {
            return addressResult.Errors;
        }

        var emailResult = SponsorFieldBuilder.BuildBusinessEmail(command.BusinessEmailAddress);

        if (emailResult.IsError)
        {
            return emailResult.Errors;
        }

        var phoneNumbersResult = SponsorFieldBuilder.BuildPhoneNumbers(command.PhoneNumbers);

        if (phoneNumbersResult.IsError)
        {
            return phoneNumbersResult.Errors;
        }

        var contactResult = SponsorFieldBuilder.BuildSponsorContact(
            command.ContactName, command.ContactPhoneType, command.ContactPhoneNumber, command.ContactPhoneExtension, command.ContactEmail);

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

        await RemoveClaimedPendingUploadAsync(sponsor.Logo, cancellationToken);

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:sponsors", token: cancellationToken);

        return new CreatedSponsor
        {
            Id = sponsor.Id,
            Slug = sponsor.Slug
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

    private async Task RemoveClaimedPendingUploadAsync(StoredFile? logo, CancellationToken cancellationToken)
    {
        if (logo is null)
        {
            return;
        }

        var claimed = await appDbContext.PendingUploads
            .Where(pending => pending.Container == logo.Container && pending.Path == logo.Path)
            .ToListAsync(cancellationToken);

        appDbContext.PendingUploads.RemoveRange(claimed);
    }
}