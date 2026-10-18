using Hangfire;

namespace Neba.Api.Legacy.Sync;

/// <summary>
/// Registry and schedule for the weekly sync that re-syncs anything the Software changed in the last
/// week, catching per-action `/legacy` calls that never landed. Deleted along with the rest of Legacy/
/// at Software sunset. To add a table, write its <c>*WeeklySyncJob</c> and add one line to <see cref="Tables"/>.
/// </summary>
internal static class WeeklyLegacySync
{
    public const string RecurringJobId = "legacy-weekly-sync";

    // Monday 08:00 in LegacyTimeZones.Eastern.
    public const string Cron = "0 8 * * 1";

    // 7 days of changes plus a day of margin for client-clock skew (Audit_UpdatedTimestamp is the
    // client PC's DateTime.Now), a late run, and the few hours between UTC and Eastern. Re-syncing a
    // row twice is harmless, so overlap costs nothing.
    public const int LookbackDays = 8;

    // Each entry enqueues one table's job on its own, so one table's failure and retry does not rerun
    // the others. Every entry gets the same `since`, so a retry reads the same window.
    public static readonly IReadOnlyList<Action<IBackgroundJobClient, DateTime>> Tables =
    [
        (jobs, since) => jobs.Enqueue<BowlerWeeklySyncJob>(job => job.SyncAsync(since, CancellationToken.None)),
    ];
}

internal sealed class WeeklyLegacySyncJob(
    IBackgroundJobClient jobs,
    TimeProvider clock,
    ILogger<WeeklyLegacySyncJob> logger)
{
    public Task RunAsync()
    {
        using var _ = LegacyActor.EnterActorScope();

        // The Software's timestamps are Eastern wall-clock time. No conversion: the extra lookback
        // day covers the 4-5 hour offset from UTC.
        var since = clock.GetUtcNow().UtcDateTime.AddDays(-WeeklyLegacySync.LookbackDays);

        logger.LogWeeklyLegacySyncStarted(since, WeeklyLegacySync.Tables.Count);

        foreach (var enqueue in WeeklyLegacySync.Tables)
        {
            enqueue(jobs, since);
        }

        return Task.CompletedTask;
    }
}

internal static partial class WeeklyLegacySyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Weekly legacy sync started: syncing {TableCount} tables changed since {Since:u}.")]
    public static partial void LogWeeklyLegacySyncStarted(this ILogger<WeeklyLegacySyncJob> logger, DateTime since, int tableCount);
}
