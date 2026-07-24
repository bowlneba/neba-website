using System.Diagnostics.CodeAnalysis;

using Neba.Api.BackgroundJobs;
using Neba.Api.Storage;

namespace Neba.Api.Features.Tournaments.EditTournament;

internal sealed class DeleteTournamentFilesJobHandler(
    IFileStorageService fileStorageService,
    ILogger<DeleteTournamentFilesJobHandler> logger)
    : IBackgroundJobHandler<DeleteTournamentFilesJob>
{
    public async Task ExecuteAsync(DeleteTournamentFilesJob job, CancellationToken cancellationToken)
    {
        foreach (var file in job.Files)
        {
            await DeleteFileAsync(file, cancellationToken);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We want to log the error and continue processing other files.")]
    private async Task DeleteFileAsync(TournamentFileReference file, CancellationToken cancellationToken)
    {
        try
        {
            await fileStorageService.DeleteAsync(file.Container, file.Path, cancellationToken);
            logger.LogDeletedTournamentFile(file.Container, file.Path);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogFailedToDeleteTournamentFile(ex, file.Container, file.Path);
        }
    }
}

internal static partial class DeleteTournamentFilesJobLogMessages
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Deleted tournament file from container '{Container}' with path '{Path}'.")]
    public static partial void LogDeletedTournamentFile(this ILogger<DeleteTournamentFilesJobHandler> logger, string container, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to delete tournament file from container '{Container}' with path '{Path}'")]
    public static partial void LogFailedToDeleteTournamentFile(this ILogger<DeleteTournamentFilesJobHandler> logger, Exception exception, string container, string path);
}
