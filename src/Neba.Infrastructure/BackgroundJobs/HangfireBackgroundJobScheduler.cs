using System.ComponentModel;
using System.Diagnostics;

using Hangfire;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Neba.Application.BackgroundJobs;
using Neba.Infrastructure.Telemetry;

namespace Neba.Infrastructure.BackgroundJobs;

internal sealed class HangfireBackgroundJobScheduler(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<HangfireBackgroundJobScheduler> logger)
        : IBackgroundJobScheduler
{
    private static readonly ActivitySource ActivitySource = new("Neba.Hangfire");

    public string Enqueue<TJob>(TJob job)
        where TJob : IBackgroundJob
    {
        logger.LogEnqueueBackgroundJob(typeof(TJob).Name);

        string jobName = GetJobDisplayName(job);

        return BackgroundJob.Enqueue<HangfireBackgroundJobScheduler>(
            scheduler => scheduler.ExecuteJobAsync(job, jobName, CancellationToken.None));
    }

    public string Schedule<TJob>(
        TJob job,
        TimeSpan delay)
            where TJob : IBackgroundJob
    {
        logger.LogScheduleBackgroundJobWithDelay(typeof(TJob).Name, delay);

        string jobName = GetJobDisplayName(job);

        return BackgroundJob.Schedule<HangfireBackgroundJobScheduler>(
            scheduler => scheduler.ExecuteJobAsync(job, jobName, CancellationToken.None),
            delay);
    }

    public string Schedule<TJob>(
        TJob job,
        DateTimeOffset enqueueAt)
            where TJob : IBackgroundJob
    {
        logger.LogScheduleBackgroundJobAt(typeof(TJob).Name, enqueueAt);

        string jobName = GetJobDisplayName(job);

        return BackgroundJob.Schedule<HangfireBackgroundJobScheduler>(
            scheduler => scheduler.ExecuteJobAsync(job, jobName, CancellationToken.None),
            enqueueAt);
    }

    public void AddOrUpdateRecurring<TJob>(
        string recurringJobId,
        TJob job,
        string cronExpression)
            where TJob : IBackgroundJob
    {
        logger.LogAddOrUpdateRecurringBackgroundJob(typeof(TJob).Name, recurringJobId, cronExpression);

        string jobName = GetJobDisplayName(job);

        RecurringJob.AddOrUpdate<HangfireBackgroundJobScheduler>(
            recurringJobId,
            scheduler => scheduler.ExecuteJobAsync(job, jobName, CancellationToken.None),
            cronExpression);
    }

    public void RemoveRecurring(string recurringJobId)
    {
        logger.LogRemoveRecurringBackgroundJob(recurringJobId);

        RecurringJob.RemoveIfExists(recurringJobId);
    }

    public string ContinueJobWith<TJob>(
        string parentJobId,
        TJob job)
            where TJob : IBackgroundJob
    {
        logger.LogContinueWithBackgroundJob(typeof(TJob).Name, parentJobId);

        string jobName = GetJobDisplayName(job);

        return BackgroundJob.ContinueJobWith<HangfireBackgroundJobScheduler>(
            parentJobId,
            scheduler => scheduler.ExecuteJobAsync(job, jobName, CancellationToken.None));
    }

    public bool Delete(string jobId)
    {
        logger.LogDeleteBackgroundJob(jobId);

        return BackgroundJob.Delete(jobId);
    }

    [DisplayName("{1}")]
    public async Task ExecuteJobAsync<TJob>(
        TJob job,
        string displayName,
        CancellationToken cancellationToken)
            where TJob : IBackgroundJob
    {
        string jobType = typeof(TJob).Name;

        using Activity? activity = ActivitySource.StartActivity($"hangfire.execute_job.{jobType}");

        activity?.SetCodeAttributes(jobType, "Neba.Hangfire");
        activity?.SetTag("job.display_name", displayName);

        long startTimestamp = Stopwatch.GetTimestamp();
        HangfireMetrics.RecordJobStart(jobType);

        try
        {
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            IBackgroundJobHandler<TJob> handler = scope.ServiceProvider.GetRequiredService<IBackgroundJobHandler<TJob>>();

            logger.LogBackgroundJobStarted(jobType);

            await handler.ExecuteAsync(job, cancellationToken);

            double durationMilliseconds = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            HangfireMetrics.RecordJobSuccess(jobType, durationMilliseconds);

            activity?.SetTag("job.duration_ms", durationMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            double durationMilliseconds = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            HangfireMetrics.RecordJobFailure(jobType, durationMilliseconds, ex.GetType().Name);

            activity?.SetTag("job.duration_ms", durationMilliseconds);
            activity?.SetExceptionTags(ex);

            throw;
        }
    }

    private static string GetJobDisplayName<TJob>(TJob job)
        where TJob : IBackgroundJob
            => job.JobName;
}

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