using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Neba.Infrastructure.BackgroundJobs;

internal static class HangfireMetrics
{
    private const string JobTypeTagName = "job.type";

    private static readonly Meter Meter = new("Neba.Hangfire");

    private static readonly Counter<long> JobExecutions
        = Meter.CreateCounter<long>(
            "neba.hangfire.job.executions", 
            description: "Counts the number of Hangfire job executions.");

    private static readonly Counter<long> JobSuccesses
        = Meter.CreateCounter<long>(
            "neba.hangfire.job.successes", 
            description: "Counts the number of successful Hangfire job executions.");

    private static readonly Counter<long> JobFailures
        = Meter.CreateCounter<long>(
            "neba.hangfire.job.failures", 
            description: "Counts the number of failed Hangfire job executions.");

    private static readonly Histogram<double> JobDuration
        = Meter.CreateHistogram<double>(
            "neba.hangfire.job.duration",
            unit: "ms", 
            description: "Records the duration of Hangfire job executions in milliseconds.");

    public static void RecordJobStart(string jobType)
    {
        TagList tags = new() { { JobTypeTagName, jobType } };

        JobExecutions.Add(1, tags);
    }

    public static void RecordJobSuccess(
        string jobType, 
        double durationMilliseconds)
    {
        TagList successTags = new() { { JobTypeTagName, jobType } };
        JobSuccesses.Add(1, successTags);

        TagList durationTags = new()
        {
            { JobTypeTagName, jobType },
            { "result", "success" }
        };

        JobDuration.Record(durationMilliseconds, durationTags);
    }

    public static void RecordJobFailure(
        string jobType,
        double durationMilliseconds,
        string errorType)
    {
        TagList failureTags = new()
        {
            { JobTypeTagName, jobType },
            { "error.type", errorType }
        };
        JobFailures.Add(1, failureTags);

        TagList durationTags = new()
        {
            { JobTypeTagName, jobType },
            { "result", "failure" },
            { "error.type", errorType }
        };
        JobDuration.Record(durationMilliseconds, durationTags);
    }
}