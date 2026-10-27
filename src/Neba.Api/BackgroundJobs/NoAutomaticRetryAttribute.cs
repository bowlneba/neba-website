namespace Neba.Api.BackgroundJobs;

/// <summary>
/// Marks a job record that Hangfire must never retry: a failure goes straight to <c>Failed</c>.
/// Hangfire reads retry attributes from the shared <c>HangfireBackgroundJobScheduler.ExecuteJobAsync</c>
/// method, so a per-job attribute on the handler has no effect; <see cref="OptOutAutomaticRetryAttribute"/>
/// looks for this one on the job argument instead.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class NoAutomaticRetryAttribute : Attribute;