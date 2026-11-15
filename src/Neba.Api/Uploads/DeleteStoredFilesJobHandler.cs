using System.Diagnostics.CodeAnalysis;

using Neba.Api.BackgroundJobs;
using Neba.Api.Storage;

namespace Neba.Api.Uploads;

internal sealed class DeleteStoredFilesJobHandler(
    IFileStorageService fileStorageService,
    ILogger<DeleteStoredFilesJobHandler> logger)
    : IBackgroundJobHandler<DeleteStoredFilesJob>
{
    public async Task ExecuteAsync(DeleteStoredFilesJob job, CancellationToken cancellationToken)
    {
        foreach (var file in job.Files)
        {
            await DeleteFileAsync(file, cancellationToken);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We want to log the error and continue processing other files.")]
    private async Task DeleteFileAsync(BlobReference file, CancellationToken cancellationToken)
    {
        try
        {
            await fileStorageService.DeleteAsync(file.Container, file.Path, cancellationToken);
            logger.LogDeletedStoredFile(file.Container, file.Path);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogFailedToDeleteStoredFile(ex, file.Container, file.Path);
        }
    }
}

internal static partial class DeleteStoredFilesJobLogMessages
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Deleted stored file from container '{Container}' with path '{Path}'.")]
    public static partial void LogDeletedStoredFile(this ILogger<DeleteStoredFilesJobHandler> logger, string container, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to delete stored file from container '{Container}' with path '{Path}'")]
    public static partial void LogFailedToDeleteStoredFile(this ILogger<DeleteStoredFilesJobHandler> logger, Exception exception, string container, string path);
}