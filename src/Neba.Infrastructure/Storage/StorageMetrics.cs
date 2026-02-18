using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Neba.Infrastructure.Storage;

internal static class StorageMetrics
{
    private const string ContainerTagName = "storage.container";
    private const string OperationTagName = "storage.operation";
    private const string ResultTagName = "result";
    private const string ErrorTypeTagName = "error.type";

    private static readonly Meter Meter = new("Neba.Storage");

    private static readonly Counter<long> OperationCount
        = Meter.CreateCounter<long>(
            "neba.storage.operation.count",
            description: "Counts the number of storage operations performed.");

    private static readonly Histogram<double> OperationDuration
        = Meter.CreateHistogram<double>(
            "neba.storage.operation.duration",
            unit: "ms",
            description: "Measures the duration of storage operations.");

    private static readonly Histogram<long> FileSize
        = Meter.CreateHistogram<long>(
            "neba.storage.file.size",
            unit: "bytes",
            description: "Measures the size of files uploaded or downloaded.");

    /// <summary>
    /// Records a successful storage operation.
    /// </summary>
    /// <param name="container">The storage container name.</param>
    /// <param name="operation">The operation type (e.g., "upload", "download", "exists").</param>
    /// <param name="durationMilliseconds">The operation duration in milliseconds.</param>
    /// <param name="sizeBytes">The file size in bytes (optional, only for upload/download).</param>
    public static void RecordOperationSuccess(
        string container,
        string operation,
        double durationMilliseconds,
        int? sizeBytes = null)
    {
        TagList countTags = new()
        {
            { ContainerTagName, container },
            { OperationTagName, operation },
            { ResultTagName, "success" }
        };
        OperationCount.Add(1, countTags);

        TagList durationTags = new()
        {
            { ContainerTagName, container },
            { OperationTagName, operation },
            { ResultTagName, "success" }
        };
        OperationDuration.Record(durationMilliseconds, durationTags);

        if (sizeBytes.HasValue)
        {
            TagList sizeTags = new()
            {
                { ContainerTagName, container },
                { OperationTagName, operation }
            };
            FileSize.Record(sizeBytes.Value, sizeTags);
        }
    }

    /// <summary>
    /// Records a failed storage operation.
    /// </summary>
    /// <param name="container">The storage container name.</param>
    /// <param name="operation">The operation type (e.g., "upload", "download", "exists").</param>
    /// <param name="durationMilliseconds">The operation duration in milliseconds.</param>
    /// <param name="errorType">The type of error that occurred (exception type name).</param>
    public static void RecordOperationFailure(
        string container,
        string operation,
        double durationMilliseconds,
        string errorType)
    {
        TagList countTags = new()
        {
            { ContainerTagName, container },
            { OperationTagName, operation },
            { ErrorTypeTagName, errorType },
            { ResultTagName, "failure" }
        };
        OperationCount.Add(1, countTags);

        TagList durationTags = new()
        {
            { ContainerTagName, container },
            { OperationTagName, operation },
            { ErrorTypeTagName, errorType },
            { ResultTagName, "failure" }
        };
        OperationDuration.Record(durationMilliseconds, durationTags);
    }
}