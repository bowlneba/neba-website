using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Neba.Website.Server.Telemetry.Metrics;

internal static class ApiMetrics
{
    private static readonly Meter Meter = new("Neba.Website.Api");

    private static readonly Counter<long> ApiCalls =
        Meter.CreateCounter<long>(
            "neba.website.api.calls",
            description: "Counts the number of API calls made from Neba.Website.Server"
        );

    private static readonly Counter<long> ApiErrors =
        Meter.CreateCounter<long>(
            "neba.website.api.errors",
            description: "Counts the number of API errors from Neba.Website.Server"
        );

    private static readonly Histogram<double> ApiDuration =
        Meter.CreateHistogram<double>(
            "neba.website.api.duration",
            unit: "ms",
            description: "Records the duration of API calls from Neba.Website.Server"
        );

    public static void RecordApiCall(string apiName, string operationName)
    {
        var tags = new TagList
        {
            { ApiMetricTagNames.ApiName, apiName },
            { ApiMetricTagNames.OperationName, operationName }
        };

        ApiCalls.Add(1, tags);
    }

    public static void RecordSuccess(string apiName, string operationName, double durationMs)
    {
        var tags = new TagList
        {
            { ApiMetricTagNames.ApiName, apiName },
            { ApiMetricTagNames.OperationName, operationName },
            { ApiMetricTagNames.ResultStatus, "success" }
        };

        ApiDuration.Record(durationMs, tags);
    }

    public static void RecordError(
        string apiName,
        string operationName,
        double durationMs,
        string errorType,
        int? httpStatusCode = null)
    {
        var errorTags = new TagList
        {
            { ApiMetricTagNames.ApiName, apiName },
            { ApiMetricTagNames.OperationName, operationName },
            { ApiMetricTagNames.ErrorType, errorType }
        };

        if (httpStatusCode.HasValue)
        {
            errorTags.Add(ApiMetricTagNames.HttpStatusCode, httpStatusCode.Value);
        }

        ApiErrors.Add(1, errorTags);

        var durationTags = new TagList
        {
            { ApiMetricTagNames.ApiName, apiName },
            { ApiMetricTagNames.OperationName, operationName },
            { ApiMetricTagNames.ResultStatus, "failure" },
            { ApiMetricTagNames.ErrorType, errorType }
        };

        ApiDuration.Record(durationMs, durationTags);
    }
}