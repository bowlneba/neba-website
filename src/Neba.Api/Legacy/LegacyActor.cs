using Neba.Api.Auditing;
using Neba.Api.Identity;

namespace Neba.Api.Legacy;

/// <summary>
/// Shared audit-attribution actor id for the Software's backdoor calls — stamped as a claim by
/// <see cref="LegacyApiKeyFilter"/> for the HTTP-level audit event, and set as the
/// <see cref="Neba.Api.Identity.AmbientActorContext"/> by each <c>*SyncJob</c> for its own
/// background-job audit events (Hangfire jobs have no HttpContext to read a claim from).
/// </summary>
internal static class LegacyActor
{
    public const string Id = "software-sync";

    /// <summary>
    /// Sets both the ambient actor (as <see cref="Id"/>) and the ambient correlation id for the
    /// remainder of a <c>*SyncJob</c>'s/award job's async call chain, in one call. Dispose the
    /// returned scope at the end of the job.
    /// </summary>
    public static IDisposable EnterAmbientContext(string correlationId)
    {
        var actorScope = AmbientActorContext.SetActor(Id);
        var correlationScope = AmbientCorrelationContext.SetCorrelationId(correlationId);
        return new CombinedScope(actorScope, correlationScope);
    }

    private sealed class CombinedScope(IDisposable actorScope, IDisposable correlationScope) : IDisposable
    {
        public void Dispose()
        {
            correlationScope.Dispose();
            actorScope.Dispose();
        }
    }
}
