using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Neba.Infrastructure.Documents;

internal static class GoogleDriveMetrics
{
    private const string DocumentNameTagName = "document.name";
    private const string DocumentIdTagName = "document.id";
    private const string ExportFormatTagName = "export.format";
    private const string ErrorTypeTagName = "error.type";

    private static readonly Meter Meter = new("Neba.GoogleDrive");

    private static readonly Counter<long> ExportCount
        = Meter.CreateCounter<long>(
            "neba.google_drive.export.count",
            description: "Counts the number of document exports performed through Google Drive.");

    private static readonly Histogram<double> ExportDuration
        = Meter.CreateHistogram<double>(
            "neba.google_drive.export.duration",
            unit: "ms",
            description: "Measures the duration of document export operations through Google Drive.");

    private static readonly Histogram<long> ExportSize
        = Meter.CreateHistogram<long>(
            "neba.google_drive.export.size",
            unit: "bytes",
            description: "Measures the size of exported documents from Google Drive.");

    /// <summary>
    /// Records a successful document export operation.
    /// </summary>
    /// <param name="documentName">The logical name of the document (e.g., "bylaws").</param>
    /// <param name="documentId">The Google Drive file ID.</param>
    /// <param name="durationMilliseconds">The export duration in milliseconds.</param>
    /// <param name="sizeBytes">The size of the exported HTML in bytes.</param>
    /// <param name="exportFormat">The export format (e.g., "text/html").</param>
    public static void RecordExportSuccess(
        string documentName,
        string documentId,
        double durationMilliseconds,
        int sizeBytes,
        string exportFormat)
    {
        TagList countTags = new()
        {
            { DocumentNameTagName, documentName },
            { "result", "success" }
        };
        ExportCount.Add(1, countTags);

        TagList durationTags = new()
        {
            { DocumentNameTagName, documentName },
            { DocumentIdTagName, documentId },
            { ExportFormatTagName, exportFormat },
            { "result", "success" }
        };
        ExportDuration.Record(durationMilliseconds, durationTags);

        TagList sizeTags = new()
        {
            { DocumentNameTagName, documentName },
            { DocumentIdTagName, documentId },
            { ExportFormatTagName, exportFormat },
        };
        ExportSize.Record(sizeBytes, sizeTags);
    }

    /// <summary>
    /// Records a failed document export operation.
    /// </summary>
    /// <param name="documentName">The logical name of the document (e.g., "bylaws").</param>
    /// <param name="documentId">The Google Drive file ID.</param>
    /// <param name="exportFormat">The export format (e.g., "text/html").</param>
    /// <param name="errorType">The type of error that occurred (exception type name).</param>
    public static void RecordExportFailure(
        string documentName,
        string documentId,
        string exportFormat,
        string errorType)
    {
        TagList countTags = new()
        {
            { DocumentNameTagName, documentName },
            { DocumentIdTagName, documentId },
            { ExportFormatTagName, exportFormat },
            { ErrorTypeTagName, errorType },
            { "result", "failure" }
        };
        ExportCount.Add(1, countTags);
    }
}