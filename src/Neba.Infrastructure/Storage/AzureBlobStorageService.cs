using System.Diagnostics;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Logging;

using Neba.Application.Clock;
using Neba.Application.Storage;
using Neba.Infrastructure.Telemetry;

namespace Neba.Infrastructure.Storage;

internal sealed class AzureBlobStorageService : IFileStorageService
{
    private const string StorageMetricsNamespace = "Neba.Storage";
    private const string StorageDurationMsTag = "storage.duration_ms";

    private static readonly ActivitySource ActivitySource = new(StorageMetricsNamespace);

    private readonly BlobServiceClient _blobServiceClient;
    private readonly IStopwatchProvider _stopwatchProvider;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        BlobServiceClient blobServiceClient,
        IStopwatchProvider stopwatchProvider,
        ILogger<AzureBlobStorageService> logger)
    {
        ArgumentNullException.ThrowIfNull(blobServiceClient);
        ArgumentNullException.ThrowIfNull(stopwatchProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _blobServiceClient = blobServiceClient;
        _stopwatchProvider = stopwatchProvider;
        _logger = logger;
    }

    public async Task<bool> ExistsAsync(string container, string path, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("storage.exists", ActivityKind.Client);
        activity?.SetCodeAttributes(nameof(ExistsAsync), StorageMetricsNamespace);
        activity?.SetTag("storage.container", container);
        activity?.SetTag("storage.path", path);

        long startTimestamp = _stopwatchProvider.GetTimestamp();

        try
        {
            _logger.LogCheckingFileExists(container, path);

            var blobClient = GetBlobClient(container, path);
            bool exists = await blobClient.ExistsAsync(cancellationToken);

            var durationMs = _stopwatchProvider.GetElapsedTime(startTimestamp);

            _logger.LogFileExistsChecked(container, path, exists, durationMs.Milliseconds);

            StorageMetrics.RecordOperationSuccess(container, "exists", durationMs.Milliseconds);

            activity?.SetTag("storage.exists", exists);
            activity?.SetTag(StorageDurationMsTag, durationMs.TotalMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return exists;
        }
        catch (Exception ex)
        {
            var durationMs = _stopwatchProvider.GetElapsedTime(startTimestamp);

            _logger.LogFileExistsCheckFailed(ex, container, path);

            StorageMetrics.RecordOperationFailure(container, "exists", durationMs.Milliseconds, ex.GetType().Name);

            activity?.SetExceptionTags(ex);
            activity?.SetTag(StorageDurationMsTag, durationMs.TotalMilliseconds);

            throw;
        }
    }

    public async Task<StoredFile?> GetFileAsync(string container, string path, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("storage.download", ActivityKind.Client);
        activity?.SetCodeAttributes(nameof(GetFileAsync), StorageMetricsNamespace);
        activity?.SetTag("storage.container", container);
        activity?.SetTag("storage.path", path);

        long startTimestamp = _stopwatchProvider.GetTimestamp();

        try
        {
            _logger.LogDownloadingFile(container, path);

            var blobClient = GetBlobClient(container, path);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                var durationMs = _stopwatchProvider.GetElapsedTime(startTimestamp);

                _logger.LogFileNotFound(container, path);

                activity?.SetTag("storage.found", false);
                activity?.SetTag(StorageDurationMsTag, durationMs.TotalMilliseconds);
                activity?.SetStatus(ActivityStatusCode.Ok);

                return null;
            }

            var result = await blobClient.DownloadContentAsync(cancellationToken);

            var storedFile = new StoredFile
            {
                Content = result.Value.Content.ToString(),
                ContentType = result.Value.Details.ContentType,
                Metadata = result.Value.Details.Metadata
            };

            var finalDurationMs = _stopwatchProvider.GetElapsedTime(startTimestamp);

            _logger.LogFileDownloaded(container, path, storedFile.Content.Length, finalDurationMs.Milliseconds);

            StorageMetrics.RecordOperationSuccess(container, "download", finalDurationMs.Milliseconds, storedFile.Content.Length);

            activity?.SetTag("storage.found", true);
            activity?.SetTag("storage.size_bytes", storedFile.Content.Length);
            activity?.SetTag("storage.content_type", storedFile.ContentType);
            activity?.SetTag(StorageDurationMsTag, finalDurationMs.TotalMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return storedFile;
        }
        catch (Exception ex)
        {
            var durationMs = _stopwatchProvider.GetElapsedTime(startTimestamp);

            _logger.LogFileDownloadFailed(ex, container, path);

            StorageMetrics.RecordOperationFailure(container, "download", durationMs.Milliseconds, ex.GetType().Name);

            activity?.SetExceptionTags(ex);
            activity?.SetTag(StorageDurationMsTag, durationMs.TotalMilliseconds);

            throw;
        }
    }

    public async Task UploadFileAsync(string container, string path, string content, string contentType, IDictionary<string, string> metadata, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("storage.upload", ActivityKind.Client);
        activity?.SetCodeAttributes(nameof(UploadFileAsync), StorageMetricsNamespace);
        activity?.SetTag("storage.container", container);
        activity?.SetTag("storage.path", path);
        activity?.SetTag("storage.size_bytes", content.Length);
        activity?.SetTag("storage.content_type", contentType);

        long startTimestamp = _stopwatchProvider.GetTimestamp();

        try
        {
            _logger.LogUploadingFile(container, path, content.Length, contentType);

            var containerClient = _blobServiceClient.GetBlobContainerClient(container);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(path);

            await blobClient.UploadAsync(
                BinaryData.FromString(content),
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    },
                    Metadata = metadata
                },
                cancellationToken);

            var durationMs = _stopwatchProvider.GetElapsedTime(startTimestamp);

            _logger.LogFileUploaded(container, path, content.Length, durationMs.Milliseconds);

            StorageMetrics.RecordOperationSuccess(container, "upload", durationMs.Milliseconds, content.Length);

            activity?.SetTag(StorageDurationMsTag, durationMs.TotalMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            var durationMs = _stopwatchProvider.GetElapsedTime(startTimestamp);

            _logger.LogFileUploadFailed(ex, container, path, content.Length);

            StorageMetrics.RecordOperationFailure(container, "upload", durationMs.Milliseconds, ex.GetType().Name);

            activity?.SetExceptionTags(ex);
            activity?.SetTag(StorageDurationMsTag, durationMs.TotalMilliseconds);

            throw;
        }
    }

    private BlobClient GetBlobClient(string container, string path)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);

        return containerClient.GetBlobClient(path);
    }
}

/// <summary>
/// Structured logging messages for Azure Blob Storage service operations.
/// </summary>
/// <remarks>
/// Uses LoggerMessage source generator for zero-allocation, high-performance logging.
/// All log methods are extension methods on ILogger for convenient usage.
/// </remarks>
internal static partial class AzureBlobStorageServiceLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Checking if file exists: {Container}/{Path}")]
    public static partial void LogCheckingFileExists(
        this ILogger<AzureBlobStorageService> logger,
        string container,
        string path);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "File exists check completed: {Container}/{Path} (exists: {Exists}, {DurationMs} ms)")]
    public static partial void LogFileExistsChecked(
        this ILogger<AzureBlobStorageService> logger,
        string container,
        string path,
        bool exists,
        double durationMs);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to check if file exists: {Container}/{Path}")]
    public static partial void LogFileExistsCheckFailed(
        this ILogger<AzureBlobStorageService> logger,
        Exception exception,
        string container,
        string path);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Downloading file: {Container}/{Path}")]
    public static partial void LogDownloadingFile(
        this ILogger<AzureBlobStorageService> logger,
        string container,
        string path);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "File not found: {Container}/{Path}")]
    public static partial void LogFileNotFound(
        this ILogger<AzureBlobStorageService> logger,
        string container,
        string path);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "File downloaded successfully: {Container}/{Path} ({SizeBytes} bytes, {DurationMs} ms)")]
    public static partial void LogFileDownloaded(
        this ILogger<AzureBlobStorageService> logger,
        string container,
        string path,
        int sizeBytes,
        double durationMs);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to download file: {Container}/{Path}")]
    public static partial void LogFileDownloadFailed(
        this ILogger<AzureBlobStorageService> logger,
        Exception exception,
        string container,
        string path);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Uploading file: {Container}/{Path} ({SizeBytes} bytes, content type: {ContentType})")]
    public static partial void LogUploadingFile(
        this ILogger<AzureBlobStorageService> logger,
        string container,
        string path,
        int sizeBytes,
        string contentType);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "File uploaded successfully: {Container}/{Path} ({SizeBytes} bytes, {DurationMs} ms)")]
    public static partial void LogFileUploaded(
        this ILogger<AzureBlobStorageService> logger,
        string container,
        string path,
        int sizeBytes,
        double durationMs);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to upload file: {Container}/{Path} ({SizeBytes} bytes)")]
    public static partial void LogFileUploadFailed(
        this ILogger<AzureBlobStorageService> logger,
        Exception exception,
        string container,
        string path,
        int sizeBytes);
}