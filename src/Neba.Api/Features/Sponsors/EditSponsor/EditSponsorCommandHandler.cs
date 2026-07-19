using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Sponsors.EditSponsor;

internal sealed class EditSponsorCommandHandler(
        AppDbContext appDbContext,
        IBackgroundJobScheduler backgroundJobScheduler,
        IFusionCache cache)
    : ICommandHandler<EditSponsorCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(EditSponsorCommand command, CancellationToken cancellationToken)
    {
        var sponsor = await appDbContext.Sponsors
            .SingleOrDefaultAsync(s => s.Id == command.SponsorId, cancellationToken);

        if (sponsor is null)
        {
            return SponsorErrors.SponsorNotFound(command.SponsorId.Value.ToString());
        }

        var fieldsResult = SponsorFieldBuilder.BuildAll(
            command.BusinessStreet, command.BusinessUnit, command.BusinessCity, command.BusinessState, command.BusinessPostalCode,
            command.BusinessEmailAddress,
            command.PhoneNumbers,
            command.ContactName, command.ContactPhoneType, command.ContactPhoneNumber, command.ContactPhoneExtension, command.ContactEmail);

        if (fieldsResult.IsError)
        {
            return fieldsResult.Errors;
        }

        var fields = fieldsResult.Value;

        // Cross-aggregate fact (CLAUDE.md "Aggregate Invariants Requiring Cross-Aggregate Data"):
        // is Title tier held by some OTHER current sponsor? Excludes this sponsor so re-saving its
        // own existing Title tier doesn't self-conflict.
        var titleSponsorshipTaken = command.Tier == SponsorTier.TitleSponsor
            && await appDbContext.Sponsors.AnyAsync(
                s => s.Id != command.SponsorId && s.IsCurrentSponsor && s.Tier == SponsorTier.TitleSponsor,
                cancellationToken);

        // Must snapshot before Update() — Logo is mutated in place, so reading it after the call
        // would return the new value, not the one being replaced (see EditArticleCommandHandler).
        var previousLogo = sponsor.Logo;

        var updateResult = sponsor.Update(
            command.Name,
            command.IsCurrentSponsor,
            command.Priority,
            command.Tier,
            command.Category,
            isTitleSponsorshipAvailable: !titleSponsorshipTaken,
            command.Logo,
            command.WebsiteUrl,
            command.TagPhrase,
            command.Description,
            command.LiveReadText,
            command.PromotionalNotes,
            command.FacebookUrl,
            command.InstagramUrl,
            fields.BusinessAddress,
            fields.BusinessEmail,
            fields.PhoneNumbers,
            fields.Contact);

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await SponsorPendingUploadCleaner.RemoveClaimedAsync(appDbContext, command.Logo, cancellationToken);

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:sponsors", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:sponsors:{sponsor.Slug}", token: cancellationToken);

        if (previousLogo is not null && previousLogo != command.Logo)
        {
            backgroundJobScheduler.Enqueue(new DeleteSponsorFilesJob
            {
                Files =
                [
                    new StoredFileReference { Container = previousLogo.Container, Path = previousLogo.Path }
                ]
            });
        }

        return Result.Updated;
    }
}