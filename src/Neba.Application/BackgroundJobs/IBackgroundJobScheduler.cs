namespace Neba.Application.BackgroundJobs;

/// <summary>
/// Represents a scheduler for managing background jobs.
/// </summary>
public interface IBackgroundJobScheduler
{
    /// <summary>
    /// Enqueues a background job for immediate execution.
    /// </summary>
    /// <typeparam name="TJob">The type of the background job.</typeparam>
    /// <param name="job">The background job to enqueue.</param>
    /// <returns>The identifier of the enqueued job.</returns>
    string Enqueue<TJob>(TJob job)
        where TJob : IBackgroundJob;

    /// <summary>
    /// Schedules a background job to be executed after a specified delay.
    /// </summary>
    /// <typeparam name="TJob">The type of the background job.</typeparam>
    /// <param name="job">The background job to schedule.</param>
    /// <param name="delay">The delay after which the job should be executed.</param>
    /// <returns>The identifier of the scheduled job.</returns>
    string Schedule<TJob>(TJob job, TimeSpan delay)
        where TJob : IBackgroundJob;

    /// <summary>
    /// Schedules a background job to be executed at a specific date and time.
    /// </summary>
    /// <typeparam name="TJob">The type of the background job.</typeparam>
    /// <param name="job">The background job to schedule.</param>
    /// <param name="enqueueAt">The date and time at which the job should be executed.</param>
    /// <returns>The identifier of the scheduled job.</returns>
    string Schedule<TJob>(TJob job, DateTimeOffset enqueueAt)
        where TJob : IBackgroundJob;

    /// <summary>
    /// Adds or updates a recurring background job with the specified cron expression.
    /// </summary>
    /// <typeparam name="TJob">The type of the background job.</typeparam>
    /// <param name="recurringJobId">The identifier of the recurring job.</param>
    /// <param name="job">The background job to add or update.</param>
    /// <param name="cronExpression">The cron expression that defines the job's schedule.</param>
    void AddOrUpdateRecurring<TJob>(string recurringJobId, TJob job, string cronExpression)
        where TJob : IBackgroundJob;

    /// <summary>
    /// Removes a recurring background job with the specified identifier.
    /// </summary>
    /// <param name="recurringJobId">The identifier of the recurring job to remove.</param>
    void RemoveRecurring(string recurringJobId);

    /// <summary>
    /// Schedules a background job to be executed after the completion of a parent job.
    /// </summary>
    /// <typeparam name="TJob">The type of the background job.</typeparam>
    /// <param name="parentJobId">The identifier of the parent job.</param>
    /// <param name="job">The background job to schedule as a continuation.</param>
    /// <returns>The identifier of the continuation job.</returns>
    string ContinueJobWith<TJob>(string parentJobId, TJob job)
        where TJob : IBackgroundJob;

    /// <summary>
    /// Deletes a background job with the specified identifier.
    /// </summary>
    /// <param name="jobId">The identifier of the job to delete.</param>
    /// <returns>True if the job was successfully deleted; otherwise, false.</returns>
    bool Delete(string jobId);
}