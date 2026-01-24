namespace Neba.Application.BackgroundJobs;

/// <summary>
/// Represents a background job in the application.
/// </summary>
public interface IBackgroundJob
{
    /// <summary>
    /// Gets the name of the background job.
    /// </summary>
    string JobName { get; }
}