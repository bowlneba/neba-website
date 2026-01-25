using Microsoft.Extensions.Logging;

namespace Neba.Infrastructure.BackgroundJobs;

internal static partial class BackgroundJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Enqueuing background job of type {JobType}.")]
    public static partial void LogEnqueueBackgroundJob(
        this ILogger<HangfireBackgroundJobScheduler> logger,
        string jobType);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Scheduling background job of type {JobType} to run after a delay of {Delay}.")]
    public static partial void LogScheduleBackgroundJobWithDelay(
        this ILogger<HangfireBackgroundJobScheduler> logger,
        string jobType,
        TimeSpan delay);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Scheduling background job of type {JobType} to run at {EnqueueAt}.")]
    public static partial void LogScheduleBackgroundJobAt(
        this ILogger<HangfireBackgroundJobScheduler> logger,
        string jobType,
        DateTimeOffset enqueueAt);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Adding or updating recurring background job with ID {RecurringJobId} of type {JobType} using cron expression '{CronExpression}'.")]
    public static partial void LogAddOrUpdateRecurringBackgroundJob(
        this ILogger<HangfireBackgroundJobScheduler> logger,
        string recurringJobId,
        string jobType,
        string cronExpression);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Removing recurring background job with ID {RecurringJobId}.")]
    public static partial void LogRemoveRecurringBackgroundJob(
        this ILogger<HangfireBackgroundJobScheduler> logger,
        string recurringJobId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Scheduling continuation background job of type {JobType} to run after parent job with ID {ParentJobId}.")]
    public static partial void LogContinueWithBackgroundJob(
        this ILogger<HangfireBackgroundJobScheduler> logger,
        string jobType,
        string parentJobId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Job with ID {JobId} has been deleted"
    )]
    public static partial void LogDeleteBackgroundJob(
        this ILogger<HangfireBackgroundJobScheduler> logger,
        string jobId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Job of type {JobType} has started"
    )]
    public static partial void LogBackgroundJobStarted(
        this ILogger logger,
        string jobType);
}