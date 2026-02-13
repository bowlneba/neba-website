using System.Diagnostics;

using OpenTelemetry;

namespace Neba.ServiceDefaults.Telemetry;

/// <summary>
/// Filters out Hangfire internal traces to reduce noise in production monitoring.
/// Hangfire generates many low-value internal traces (polling, state transitions, etc.)
/// that increase monitoring costs without providing actionable insights.
/// </summary>
internal sealed class HangfireTraceFilterProcessor
    : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Filter out Hangfire-related database operations
        // Most Hangfire traces come from Npgsql executing internal queries
        if (activity.Kind == ActivityKind.Client &&
            activity.OperationName == "postgresql" &&
            activity.GetTagItem("db.query.text") is string queryText &&
            IsHangfireInternalQuery(queryText))
        {
            // Suppress this trace
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            return;
        }

        // Also filter direct Hangfire activity traces (if any)
        if (activity.Source.Name.StartsWith("Hangfire", StringComparison.OrdinalIgnoreCase))
        {
            var operationName = activity.DisplayName ?? activity.OperationName;
            var shouldKeep = operationName.Contains("Execute", StringComparison.OrdinalIgnoreCase)
                || operationName.Contains("Perform", StringComparison.OrdinalIgnoreCase)
                || activity.Status == ActivityStatusCode.Error;

            if (!shouldKeep)
            {
                activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            }
        }
    }

    private static bool IsHangfireInternalQuery(string queryText)
    {
        // Filter connection validation queries (used by connection pools and Hangfire)
        // These provide no actionable insights and create noise
        if (queryText.Equals("SELECT 1;", StringComparison.Ordinal))
        {
            return true;
        }

        // Don't filter non-Hangfire queries
        if (!queryText.Contains("\"hangfire\".", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // KEEP (don't filter) job-related operations that indicate actual work:
        // - Operations on "job" table (job creation/execution)
        // - Operations on "jobparameter" table (job arguments)
        // - INSERT into jobqueue (enqueueing jobs)
        if (queryText.Contains("\"hangfire\".\"job\"", StringComparison.OrdinalIgnoreCase) ||
            queryText.Contains("\"hangfire\".\"jobparameter\"", StringComparison.OrdinalIgnoreCase) ||
            (queryText.Contains("INSERT", StringComparison.OrdinalIgnoreCase)
                && queryText.Contains("\"hangfire\".\"jobqueue\"", StringComparison.OrdinalIgnoreCase)))
        {
            return false; // Don't filter - this is actual business activity
        }

        // FILTER internal housekeeping operations:
        // - Lock management (polling for locks, acquiring locks)
        // - Queue polling (SELECT/UPDATE from jobqueue)
        // - Cleanup (DELETE from expired records)
        // - Heartbeats (server table operations)
        // - State transitions (routine state changes)
        return queryText.Contains("\"hangfire\".\"lock\"", StringComparison.OrdinalIgnoreCase)
            || queryText.Contains("\"hangfire\".\"list\"", StringComparison.OrdinalIgnoreCase)
            || queryText.Contains("\"hangfire\".\"set\"", StringComparison.OrdinalIgnoreCase)
            || queryText.Contains("\"hangfire\".\"hash\"", StringComparison.OrdinalIgnoreCase)
            || queryText.Contains("\"hangfire\".\"counter\"", StringComparison.OrdinalIgnoreCase)
            || (queryText.Contains("\"hangfire\".\"jobqueue\"", StringComparison.OrdinalIgnoreCase)
                && (queryText.Contains("SELECT", StringComparison.OrdinalIgnoreCase) || queryText.Contains("UPDATE", StringComparison.OrdinalIgnoreCase)))
            || queryText.Contains("\"hangfire\".\"server\"", StringComparison.OrdinalIgnoreCase)
            || queryText.Contains("\"hangfire\".\"state\"", StringComparison.OrdinalIgnoreCase);
    }
}