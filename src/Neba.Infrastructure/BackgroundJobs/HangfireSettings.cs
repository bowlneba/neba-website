using System.ComponentModel.DataAnnotations;

namespace Neba.Infrastructure.BackgroundJobs;

/// <summary>
/// Settings for configuring Hangfire background job processing.
/// </summary>
public sealed record HangfireSettings
{
    /// <summary>
    /// The number of worker threads to process background jobs.
    /// </summary>
    [Range(1, 100, ErrorMessage = "WorkerCount must be between 1 and 100.")]
    public required int WorkerCount { get; init; }

    /// <summary>
    /// The number of days to retain succeeded jobs.
    /// </summary>
    [Range(1, 365, ErrorMessage = "SucceededJobsRetentionDays must be between 1 and 365.")]
    public required int SucceededJobsRetentionDays { get; init; }

    /// <summary>
    /// The number of days to retain deleted jobs.
    /// </summary>
    [Range(1, 365, ErrorMessage = "DeletedJobsRetentionDays must be between 1 and 365.")]
    public required int DeletedJobsRetentionDays { get; init; }

    /// <summary>
    /// The number of days to retain failed jobs.
    /// </summary>
    [Range(1, 365, ErrorMessage = "FailedJobsRetentionDays must be between 1 and 365.")]
    public required int FailedJobsRetentionDays { get; init; }

    /// <summary>
    /// The number of automatic retry attempts for failed jobs.
    /// </summary>
    [Range(1, 10, ErrorMessage = "AutomaticRetryAttempts must be between 1 and 10.")]
    public required int AutomaticRetryAttempts { get; init; }
}