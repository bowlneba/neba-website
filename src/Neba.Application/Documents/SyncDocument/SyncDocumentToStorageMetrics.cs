using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Neba.Application.Documents.SyncDocument;

internal static class SyncDocumentToStorageMetrics
{
    private const string DocumentNameTag = "document.name";

    private static readonly Meter Meter = new("Neba.BackgroundJobs");

    private static readonly Counter<long> JobExecutions
        = Meter.CreateCounter<long>("neba.background_job.sync_document.executions",
        description: "Number of document sync job executions");

    private static readonly Counter<long> JobSuccesses
        = Meter.CreateCounter<long>("neba.backgroundjob.sync_document.successes",
        description: "Number of successful document sync job executions");

    private static readonly Counter<long> JobFailures
        = Meter.CreateCounter<long>("neba.backgroundjob.sync_document.failures",
        description: "Number of failed document sync job executions");

    private static readonly Histogram<double> JobDuration
        = Meter.CreateHistogram<double>("neba.backgroundjob.sync_document.duration",
        unit: "ms",
        description: "Duration of document sync job executions in milliseconds");

    private static readonly Histogram<double> RetrieveDuration
        = Meter.CreateHistogram<double>("neba.backgroundjob.sync_document.retrieve.duration",
        unit: "ms",
        description: "Duration of document retrieval in milliseconds");

    private static readonly Histogram<double> UploadDuration
        = Meter.CreateHistogram<double>("neba.backgroundjob.sync_document.upload.duration",
        unit: "ms",
        description: "Duration of document upload in milliseconds");

    public static void RecordJobStarting(string documentName, string triggeredBy)
    {
        var tags = new TagList
        {
            { DocumentNameTag, documentName },
            { "triggered.by", triggeredBy }
        };
        JobExecutions.Add(1, tags);
    }

    public static void RecordJobSuccess(string documentName, double durationMs)
    {
        var tags = new TagList
        {
            { DocumentNameTag, documentName }
        };
        JobSuccesses.Add(1, tags);

        var durationTags = new TagList
        {
            { DocumentNameTag, documentName },
            { "result", "success" }
        };
        JobDuration.Record(durationMs, durationTags);
    }

    public static void RecordJobFailure(string documentName, double durationMs, string errorType)
    {
        var tags = new TagList
        {
            { DocumentNameTag, documentName },
            { "error.type", errorType }
        };
        JobFailures.Add(1, tags);

        var durationTags = new TagList
        {
            { DocumentNameTag, documentName },
            { "result", "failure" },
            { "error.type", errorType }
        };
        JobDuration.Record(durationMs, durationTags);
    }

    public static void RecordRetrieveDuration(string documentName, double durationMs)
    {
        var tags = new TagList
        {
            { DocumentNameTag, documentName }
        };
        RetrieveDuration.Record(durationMs, tags);
    }

    public static void RecordUploadDuration(string documentName, double durationMs)
    {
        var tags = new TagList
        {
            { DocumentNameTag, documentName }
        };
        UploadDuration.Record(durationMs, tags);
    }
}