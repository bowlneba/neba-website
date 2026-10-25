using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Storage.Domain;

namespace Neba.Api.Uploads;

internal static class StoredFileCleanup
{
    /// <summary>
    /// Queues deletion of the previous file when the entity no longer points at it.
    /// Files are the same when container and path match; content type and size do not count.
    /// </summary>
    public static void ScheduleDeleteOfReplaced(
        IBackgroundJobScheduler backgroundJobScheduler,
        StoredFile? previous,
        StoredFile? current)
    {
        if (previous is null || StoredFileClaim.IsSameFile(previous, current))
        {
            return;
        }

        backgroundJobScheduler.Enqueue(new DeleteStoredFilesJob
        {
            Files = [new BlobReference { Container = previous.Container, Path = previous.Path }]
        });
    }
}