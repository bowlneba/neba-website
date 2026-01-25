namespace Neba.Application.BackgroundJobs;

/// <summary>
/// Represents a handler for processing background jobs.
/// </summary>
public interface IBackgroundJobHandler<in TJob>
    where TJob : IBackgroundJob
{
    /// <summary>
    /// Executes the specified background job.
    /// </summary>
    /// <param name="job">The background job to execute.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExecuteAsync(TJob job, CancellationToken cancellationToken);
}