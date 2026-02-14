using System.Diagnostics;
using System.Net.Mime;
using System.Text.Json;

using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

using Microsoft.Extensions.Logging;

using Neba.Application.Clock;
using Neba.Application.Documents;
using Neba.Infrastructure.Telemetry;

namespace Neba.Infrastructure.Documents;

internal sealed class GoogleDriveService(
    GoogleDriveSettings settings,
    HtmlProcessor htmlProcessor,
    IStopwatchProvider stopwatchProvider,
    ILogger<GoogleDriveService> logger)
        : IDocumentsService, IDisposable
{
    private static readonly ActivitySource ActivitySource = new("Neba.Documents");

    private readonly GoogleDriveSettings _settings = settings;
    private readonly HtmlProcessor _htmlProcessor = htmlProcessor;
    private readonly DriveService _driveService = CreateDriveService(settings);
    private readonly IStopwatchProvider _stopwatchProvider = stopwatchProvider;
    private readonly ILogger<GoogleDriveService> _logger = logger;

    /// <summary>
    /// Retrieves a document as HTML by its configured name.
    /// </summary>
    /// <param name="documentName">The logical name of the document (e.g., "bylaws").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document content as processed HTML.</returns>
    /// <exception cref="InvalidOperationException">Thrown when document name is not found in configuration.</exception>
    /// <exception cref="GoogleApiException">Thrown when Google Drive API request fails.</exception>
    public async Task<string> GetDocumentAsHtmlAsync(string documentName, CancellationToken cancellationToken)
    {
        // Find document configuration by name (case-insensitive)
        var document = _settings.Documents
            .FirstOrDefault(d => string.Equals(d.Name, documentName, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException(
                    $"Document with name '{documentName}' not found in configuration.");

        using var activity = ActivitySource.StartActivity("document.export", ActivityKind.Client);
        activity?.SetCodeAttributes(nameof(GetDocumentAsHtmlAsync), "Neba.Documents");
        activity?.SetTag("document.name", documentName);
        activity?.SetTag("document.id", document.DocumentId);
        activity?.SetTag("export.format", MediaTypeNames.Text.Html);

        long startTimestamp = _stopwatchProvider.GetTimestamp();

        try
        {
            _logger.LogExportingDocument(documentName, document.DocumentId);
            activity?.AddEvent(new ActivityEvent("export_started"));

            // Export document as HTML from Google Drive
            var exportRequest = _driveService.Files.Export(
                document.DocumentId,
                MediaTypeNames.Text.Html);

            await using var stream = new MemoryStream();
            await exportRequest.DownloadAsync(stream, cancellationToken);

            // Convert stream to string
            var rawHtml = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            var originalSize = rawHtml.Length;

            _logger.LogProcessingHtml(documentName, originalSize);
            activity?.AddEvent(new ActivityEvent("html_processing_started"));

            // Post-process HTML to clean up Google Drive export artifacts
            var processedHtml = _htmlProcessor.Process(rawHtml);
            var processedSize = processedHtml.Length;

            activity?.AddEvent(new ActivityEvent("html_processing_completed"));
            _logger.LogHtmlProcessed(documentName, processedSize);

            // Calculate duration
            var durationMs = _stopwatchProvider.GetElapsedTime(startTimestamp);
            _logger.LogDocumentExported(documentName, processedSize, durationMs.Milliseconds);
            activity?.AddEvent(new ActivityEvent("export_completed"));

            // Record telemetry
            GoogleDriveMetrics.RecordExportSuccess(
                documentName,
                document.DocumentId,
                durationMs.Milliseconds,
                processedSize,
                MediaTypeNames.Text.Html
            );

            activity?.SetTag("export.size_bytes", processedSize);
            activity?.SetTag("export.duration_ms", durationMs);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return processedHtml;
        }
        catch (Exception ex)
        {
            var durationMs = _stopwatchProvider.GetElapsedTime(startTimestamp);

            _logger.LogDocumentExportFailed(ex, documentName, document.DocumentId);

            GoogleDriveMetrics.RecordExportFailure(
                documentName,
                document.DocumentId,
                MediaTypeNames.Text.Html,
                ex.GetType().Name);

            activity?.SetExceptionTags(ex);
            activity?.SetTag("export.duration_ms", durationMs);

            throw;
        }
    }

    /// <summary>
    /// Creates and initializes a DriveService with authenticated credentials.
    /// </summary>
    /// <param name="settings">Google Drive configuration settings.</param>
    /// <returns>Initialized DriveService instance.</returns>
    /// <remarks>
    /// <para>
    /// This method constructs a service account credential from the minimal configuration fields
    /// and adds the standard Google auth URLs required by the credential factory.
    /// </para>
    /// <para>
    /// Why we construct JSON manually: Google's credential factory requires a full service account
    /// JSON structure with 11 fields, but most are constants that never change across Google Cloud
    /// projects. To simplify configuration, we only store the 4 essential fields (ProjectId,
    /// PrivateKeyId, PrivateKey, ClientEmail) and construct the rest programmatically.
    /// </para>
    /// </remarks>
    private static DriveService CreateDriveService(GoogleDriveSettings settings)
    {
        var credentialJson = new
        {
            type = "service_account",
            project_id = settings.Credentials.ProjectId,
            private_key_id = settings.Credentials.PrivateKeyId,
            private_key = settings.Credentials.PrivateKey.Replace("\\n", "\n", StringComparison.InvariantCulture),
            client_email = settings.Credentials.ClientEmail,
            client_id = string.Empty, // Not required for service account auth
            auth_uri = "https://accounts.google.com/o/oauth2/auth",
            token_uri = "https://oauth2.googleapis.com/token",
            auth_provider_x509_cert_url = "https://www.googleapis.com/oauth2/v1/certs",
            client_x509_cert_url = $"https://www.googleapis.com/robot/v1/metadata/x509/{Uri.EscapeDataString(settings.Credentials.ClientEmail)}",
            universe_domain = "googleapis.com"
        };

        var credentialJsonString = JsonSerializer.Serialize(credentialJson);
        var serviceAccountCredential = CredentialFactory.FromJson<ServiceAccountCredential>(credentialJsonString);
        var credential = serviceAccountCredential
            .ToGoogleCredential()
            .CreateScoped(DriveService.Scope.DriveReadonly);

        // Initialize Drive Service
        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = settings.ApplicationName
        });
    }

    public void Dispose()
        => _driveService?.Dispose();
}

/// <summary>
/// Structured logging messages for Google Drive service operations.
/// </summary>
/// <remarks>
/// Uses LoggerMessage source generator for zero-allocation, high-performance logging.
/// All log methods are extension methods on ILogger for convenient usage.
/// </remarks>
internal static partial class GoogleDriveServiceLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Exporting Google Drive document: {DocumentName} (ID: {DocumentId})")]
    public static partial void LogExportingDocument(
        this ILogger<GoogleDriveService> logger,
        string documentName,
        string documentId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Document exported successfully: {DocumentName} ({SizeBytes} bytes, {DurationMs} ms)")]
    public static partial void LogDocumentExported(
        this ILogger<GoogleDriveService> logger,
        string documentName,
        int sizeBytes,
        double durationMs);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to export document: {DocumentName} (ID: {DocumentId}))")]
    public static partial void LogDocumentExportFailed(
        this ILogger<GoogleDriveService> logger,
        Exception exception,
        string documentName,
        string documentId);

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Process HTML for document: {DocumentName} (original size: {OriginalSize} bytes)")]
    public static partial void LogProcessingHtml(
        this ILogger<GoogleDriveService> logger,
        string documentName,
        int originalSize);

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "HTML processed for document: {DocumentName} (processed size: {ProcessedSize} bytes)")]
    public static partial void LogHtmlProcessed(
        this ILogger<GoogleDriveService> logger,
        string documentName,
        int processedSize);
}