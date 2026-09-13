using Hangfire.Client;
using Hangfire.Server;

using Neba.Api.Auditing;

namespace Neba.Api.BackgroundJobs;

/// <summary>
/// Propagates the correlation id into every Hangfire job automatically, following the same
/// capture-at-creation/restore-at-performance pattern as Hangfire's own
/// <c>CaptureCultureAttribute</c>. Removes the need for each job to accept a correlationId
/// parameter and for every Enqueue/Schedule call site to thread it through by hand - a job added
/// later gets this for free, with nothing to forget.
/// </summary>
internal sealed class CorrelationIdJobFilter : IClientFilter, IServerFilter
{
    private const string ParameterName = "CorrelationId";
    private const string ScopeItemKey = "CorrelationIdJobFilter.Scope";

    public void OnCreating(CreatingContext context)
    {
        // Prefer the already-ambient id over minting a new one, so a job that schedules further
        // jobs (e.g. CompleteSeasonSyncJob scheduling its award jobs) propagates the same
        // correlation id to its children instead of each getting its own.
        var correlationId = AmbientCorrelationContext.CorrelationId
            ?? AmbientCorrelationContext.Capture(httpContext: null);

        context.SetJobParameter(ParameterName, correlationId);
    }

    public void OnCreated(CreatedContext context)
    {
        // No-op: nothing left to do once the job has been created.
    }

    public void OnPerforming(PerformingContext context)
    {
        var correlationId = context.GetJobParameter<string>(ParameterName, allowStale: true);
        if (correlationId is not null)
        {
            context.Items[ScopeItemKey] = AmbientCorrelationContext.SetCorrelationId(correlationId);
        }
    }

    public void OnPerformed(PerformedContext context)
    {
        if (context.Items.TryGetValue(ScopeItemKey, out var scope) && scope is IDisposable disposable)
        {
            disposable.Dispose();
            context.Items.Remove(ScopeItemKey);
        }
    }
}