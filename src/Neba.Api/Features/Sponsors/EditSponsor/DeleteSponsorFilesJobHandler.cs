using System.Diagnostics.CodeAnalysis;

using Neba.Api.BackgroundJobs;
using Neba.Api.Storage;

namespace Neba.Api.Features.Sponsors.EditSponsor;

internal sealed class DeleteSponsorFilesJobHandler(
    IFileStorageService fileStorageService,
    ILogger<DeleteSponsorFilesJobHandler> logger)
    : IBackgroundJobHandler<DeleteSponsorFilesJob>
{
    public async Task ExecuteAsync(DeleteSponsorFilesJob job, CancellationToken cancellationToken)
    {
        foreach (var file in job.Files)
        {
            await DeleteFileAsync(file, cancellationToken);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We want to log the error and continue processing other files.")]
    private async Task DeleteFileAsync(StoredFileReference file, CancellationToken cancellationToken)
    {
        try
        {
            await fileStorageService.DeleteAsync(file.Container, file.Path, cancellationToken);
            logger.LogDeletedSponsorFile(file.Container, file.Path);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogFailedToDeleteSponsorFile(ex, file.Container, file.Path);
        }
    }
}

internal static partial class DeleteSponsorFilesJobLogMessages
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Deleted sponsor file from container '{Container}' with path '{Path}'.")]
    public static partial void LogDeletedSponsorFile(this ILogger<DeleteSponsorFilesJobHandler> logger, string container, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to delete sponsor file from container '{Container}' with path '{Path}'")]
    public static partial void LogFailedToDeleteSponsorFile(this ILogger<DeleteSponsorFilesJobHandler> logger, Exception exception, string container, string path);
}
