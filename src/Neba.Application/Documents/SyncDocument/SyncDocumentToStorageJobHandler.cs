using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Neba.Application.BackgroundJobs;
using Neba.Application.Clock;
using Neba.Application.Storage;

namespace Neba.Application.Documents.SyncDocument;

internal sealed class SyncDocumentToStorageJobHandler(
    IDocumentsService documentsService,
    IFileStorageService fileStorageService,
    IStopwatchProvider stopwatchProvider,
    IDateTimeProvider dateTimeProvider,
    ILogger<SyncDocumentToStorageJobHandler> logger)
        : IBackgroundJobHandler<SyncDocumentToStorageJob>
{
    private readonly IDocumentsService _documentsService = documentsService;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly IStopwatchProvider _stopwatchProvider = stopwatchProvider;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly ILogger<SyncDocumentToStorageJobHandler> _logger = logger;

    private const string Container = "documents";
    private static readonly ActivitySource ActivitySource = new("Neba.BackgroundJobs");

    public async Task ExecuteAsync(SyncDocumentToStorageJob job, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("backgroundjob.sync_document");
        activity?.SetTag("document.name", job.DocumentName);
        activity?.SetTag("triggered.by", job.TriggeredBy);

        var jobStartTimestamp = _stopwatchProvider.GetTimestamp();
        SyncDocumentToStorageMetrics.RecordJobStarting(job.DocumentName, job.TriggeredBy);

        try
        {
            _logger.LogStartingSyncToStorage(job.DocumentName);

            var retrieveStartTimestamp = _stopwatchProvider.GetTimestamp();
            var document = await _documentsService.GetDocumentAsHtmlAsync(job.DocumentName, cancellationToken);
            var retrieveDuration = _stopwatchProvider.GetElapsedTime(retrieveStartTimestamp);
            SyncDocumentToStorageMetrics.RecordRetrieveDuration(job.DocumentName, retrieveDuration.Milliseconds);

            if (document is null)
            {
                _logger.LogDocumentNotFoundDuringSync(job.DocumentName);

                var notFoundDuration = _stopwatchProvider.GetElapsedTime(jobStartTimestamp);
                SyncDocumentToStorageMetrics.RecordJobFailure(job.DocumentName, notFoundDuration.Milliseconds, "DocumentNotFound");
                activity?.SetStatus(ActivityStatusCode.Error, "Document not found");

                return;
            }

            var uploadStartTimestamp = _stopwatchProvider.GetTimestamp();
            await _fileStorageService.UploadFileAsync(
                Container,
                job.DocumentName,
                document.Content,
                document.ContentType,
                new Dictionary<string, string>
                {
                    {"source_document_id", document.Id},
                    {"cached_at", _dateTimeProvider.UtcNow.ToString("o")},
                    {"source_last_modified", document.ModifiedAt?.ToString("o") ?? string.Empty}
                },
                cancellationToken
            );

            var uploadDuration = _stopwatchProvider.GetElapsedTime(uploadStartTimestamp);
            SyncDocumentToStorageMetrics.RecordJobSuccess(job.DocumentName, uploadDuration.Milliseconds);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogCompletedSyncToStorage(job.DocumentName);
        }
        catch (Exception ex)
        {
            var totalDuration = _stopwatchProvider.GetElapsedTime(jobStartTimestamp);
            SyncDocumentToStorageMetrics.RecordJobFailure(job.DocumentName, totalDuration.Milliseconds, ex.GetType().Name);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            _logger.LogErrorDuringSyncToStorage(ex, job.DocumentName);

            throw;
        }
    }
}

internal static partial class SyncDocumentToStorageLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Starting synchronization of document '{DocumentName}' to storage.")]
    public static partial void LogStartingSyncToStorage(
        this ILogger<SyncDocumentToStorageJobHandler> logger,
        string documentName);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Completed synchronization of document '{DocumentName}' to storage.")]
    public static partial void LogCompletedSyncToStorage(
        this ILogger<SyncDocumentToStorageJobHandler> logger,
        string documentName);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Document '{DocumentName}' was not found in the documents service during sync.")]
    public static partial void LogDocumentNotFoundDuringSync(
        this ILogger<SyncDocumentToStorageJobHandler> logger,
        string documentName);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error occurred during synchronization of document '{DocumentName}' to storage.")]
    public static partial void LogErrorDuringSyncToStorage(
        this ILogger<SyncDocumentToStorageJobHandler> logger,
        Exception ex,
        string documentName);
}