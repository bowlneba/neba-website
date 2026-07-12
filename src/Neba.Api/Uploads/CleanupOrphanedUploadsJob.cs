using Neba.Api.BackgroundJobs;

namespace Neba.Api.Uploads;

internal sealed record CleanupOrphanedUploadsJob
    : IBackgroundJob
{
    public string JobName
        => "Cleanup Orphaned Uploads Job";
}