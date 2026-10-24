using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Messaging;
using Neba.Api.Uploads;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.HallOfFame.EditHallOfFamePhoto;

internal sealed class EditHallOfFamePhotoCommandHandler(
        AppDbContext appDbContext,
        IFusionCache cache,
        IBackgroundJobScheduler backgroundJobScheduler)
    : ICommandHandler<EditHallOfFamePhotoCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(EditHallOfFamePhotoCommand command, CancellationToken cancellationToken)
    {
        var induction = await appDbContext.HallOfFameInductions
            .SingleOrDefaultAsync(i => i.Id == command.HallOfFameId, cancellationToken);

        if (induction is null)
        {
            return HallOfFameErrors.InductionNotFound(command.HallOfFameId);
        }

        // Snapshot before the change: the photo is replaced on the aggregate, and the old blob must be cleaned up.
        var previousPhoto = induction.Photo;

        var applied = await ApplyAsync(induction, command, previousPhoto, cancellationToken);
        if (applied.IsError)
        {
            return applied.Errors;
        }

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:hall-of-fame:inductions", token: cancellationToken);

        StoredFileCleanup.ScheduleDeleteOfReplaced(backgroundJobScheduler, previousPhoto, induction.Photo);

        return Result.Updated;
    }

    private async Task<ErrorOr<Success>> ApplyAsync(
        HallOfFameInduction induction,
        EditHallOfFamePhotoCommand command,
        StoredFile? previousPhoto,
        CancellationToken cancellationToken)
    {
        if (command.Photo is null)
        {
            // No photo and no framing: remove. Framing alone keeps the current photo and changes the framing.
            if (command.FocalX is null || command.FocalY is null || command.Zoom is null)
            {
                induction.ClearPhoto();
                return Result.Success;
            }

            return induction.SetFocalPoint(command.FocalX.Value, command.FocalY.Value, command.Zoom.Value);
        }

        // Re-save of the current photo: only the framing can change.
        if (StoredFileClaim.IsSameFile(command.Photo, previousPhoto))
        {
            var current = induction.PhotoFocalPoint ?? PhotoFocalPoint.Default;
            return induction.SetFocalPoint(
                command.FocalX ?? current.X,
                command.FocalY ?? current.Y,
                command.Zoom ?? current.Zoom);
        }

        var claim = await StoredFileClaim.EnsureClaimableAsync(
            appDbContext, command.Photo, previousPhoto, HallOfFameErrors.PhotoNotUploaded, cancellationToken);
        if (claim.IsError)
        {
            return claim.Errors;
        }

        var result = induction.SetPhoto(
            command.Photo,
            command.FocalX ?? PhotoFocalPoint.Default.X,
            command.FocalY ?? PhotoFocalPoint.Default.Y,
            command.Zoom ?? PhotoFocalPoint.Default.Zoom);
        if (result.IsError)
        {
            return result.Errors;
        }

        await PendingUploadCleaner.RemoveClaimedAsync(appDbContext, command.Photo, cancellationToken);
        return Result.Success;
    }
}
