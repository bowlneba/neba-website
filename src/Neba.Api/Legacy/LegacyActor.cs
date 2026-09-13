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
    /// Sets the ambient actor (as <see cref="Id"/>) for the remainder of a <c>*SyncJob</c>'s/award
    /// job's async call chain. Dispose the returned scope at the end of the job. The correlation
    /// id is set automatically for every job by <see cref="Neba.Api.BackgroundJobs.CorrelationIdJobFilter"/>
    /// and doesn't need a matching call here.
    /// </summary>
    public static IDisposable EnterActorScope() => AmbientActorContext.SetActor(Id);
}