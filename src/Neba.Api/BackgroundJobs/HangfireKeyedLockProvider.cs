using Hangfire;
using Hangfire.Storage;

namespace Neba.Api.BackgroundJobs;

// The same mechanism HangfireBackgroundJobScheduler.EnqueueOnce uses. The Postgres lock blocks its thread while
// it polls, so acquisition runs on the thread pool.
internal sealed class HangfireKeyedLockProvider
    : IKeyedLockProvider
{
    public Task<IDisposable?> TryAcquireAsync(string key, TimeSpan timeout, CancellationToken cancellationToken)
        => Task.Run<IDisposable?>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var connection = JobStorage.Current.GetConnection();
            try
            {
                return new Handle(connection, connection.AcquireDistributedLock(key, timeout));
            }
            catch (DistributedLockTimeoutException)
            {
                connection.Dispose();
                return null;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }, cancellationToken);

    private sealed class Handle(IStorageConnection connection, IDisposable inner)
        : IDisposable
    {
        public void Dispose()
        {
            inner.Dispose();
            connection.Dispose();
        }
    }
}