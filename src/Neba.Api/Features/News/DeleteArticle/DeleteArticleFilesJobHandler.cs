using System.Diagnostics.CodeAnalysis;

using Neba.Api.BackgroundJobs;
using Neba.Api.Storage;

namespace Neba.Api.Features.News.DeleteArticle;

internal sealed class DeleteArticleFilesJobHandler(
        IFileStorageService fileStorageService,
        ILogger<DeleteArticleFilesJobHandler> logger)
    : IBackgroundJobHandler<DeleteArticleFilesJob>
{
    public async Task ExecuteAsync(DeleteArticleFilesJob job, CancellationToken cancellationToken)
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

            logger.LogDeletedArticleFile(file.Container, file.Path);
        }
        catch (Exception ex)
        {
            logger.LogFailedToDeleteArticleFile(ex, file.Container, file.Path);
        }
    }
}

internal static partial class DeleteArticleFilesJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Deleted article file from container '{Container}' with path '{Path}'.")]
    public static partial void LogDeletedArticleFile(
        this ILogger<DeleteArticleFilesJobHandler> logger,
        string container,
        string path);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to delete article file from container '{Container}' with path '{Path}'")]
    public static partial void LogFailedToDeleteArticleFile(
        this ILogger<DeleteArticleFilesJobHandler> logger,
        Exception exception,
        string container,
        string path);
}