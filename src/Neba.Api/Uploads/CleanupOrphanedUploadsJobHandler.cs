using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Storage;

namespace Neba.Api.Uploads;

internal sealed class CleanupOrphanedUploadsJobHandler(
        AppDbContext appDbContext,
        IFileStorageService fileStorageService,
        TimeProvider timeProvider,
        ILogger<CleanupOrphanedUploadsJobHandler> logger)
    : IBackgroundJobHandler<CleanupOrphanedUploadsJob>
{
    private static readonly TimeSpan OrphanThreshold = TimeSpan.FromHours(24);

    public async Task ExecuteAsync(CleanupOrphanedUploadsJob _, CancellationToken cancellationToken)
    {
        var cutoffUtc = timeProvider.GetUtcNow() - OrphanThreshold;

        var orphans = await appDbContext.PendingUploads
            .Where(upload => upload.UploadedAtUtc < cutoffUtc)
            .ToListAsync(cancellationToken);

        foreach (var orphan in orphans)
        {
            await DeleteOrphanAsync(orphan, cancellationToken);
        }

        await appDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task DeleteOrphanAsync(PendingUpload orphan, CancellationToken cancellationToken)
    {
        var deleted = await fileStorageService.DeleteAsync(orphan.Container, orphan.Path, cancellationToken);

        if (!deleted)
        {
            logger.LogFailedToDeleteOrphanedFile(orphan.Container, orphan.Path);

            return;
        }

        appDbContext.PendingUploads.Remove(orphan);
    }
}

internal static partial class CleanupOrphanedUploadsJobHandlerLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to delete orphaned upload block {Container}/{Path}; leaving PendingUpload row for the next sweep.")]
    public static partial void LogFailedToDeleteOrphanedFile(this ILogger<CleanupOrphanedUploadsJobHandler> logger, string container, string path);
}