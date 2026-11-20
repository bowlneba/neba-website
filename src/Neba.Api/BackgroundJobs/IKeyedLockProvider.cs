namespace Neba.Api.BackgroundJobs;

/// <summary>A distributed lock per string key, shared by every app instance through Hangfire storage.</summary>
public interface IKeyedLockProvider
{
    /// <summary>Waits up to <paramref name="timeout"/> for the lock. Returns the handle to dispose, or null on timeout.</summary>
    Task<IDisposable?> TryAcquireAsync(string key, TimeSpan timeout, CancellationToken cancellationToken);
}