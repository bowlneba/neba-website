namespace Neba.Api.Identity;

/// <summary>
/// Ambient actor override for audit attribution outside an HTTP request — e.g. a Hangfire
/// background job acting on behalf of a known caller that has no <c>HttpContext</c> to read a
/// claim from. AsyncLocal-backed rather than DI-scoped: it needs to be readable by
/// <see cref="CurrentUserService"/> instances that <c>AuditEnrichmentAction</c> constructs
/// manually (that action is a singleton, so it can't take a scoped dependency), and it needs to
/// flow forward through the rest of a job's async call chain the same way
/// <c>IHttpContextAccessor</c> flows through a request's.
/// </summary>
internal static class AmbientActorContext
{
    private static readonly AsyncLocal<string?> CurrentActorId = new();

    public static string? ActorId => CurrentActorId.Value;

    /// <summary>
    /// Sets the ambient actor for the remainder of the current async call chain. Dispose the
    /// returned scope when the actor no longer applies (e.g. at the end of a background job).
    /// </summary>
    public static IDisposable SetActor(string actorId)
    {
        CurrentActorId.Value = actorId;
        return new ActorScope();
    }

    private sealed class ActorScope : IDisposable
    {
        public void Dispose() => CurrentActorId.Value = null;
    }
}
