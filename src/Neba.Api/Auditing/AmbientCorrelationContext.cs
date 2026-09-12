namespace Neba.Api.Auditing;

/// <summary>
/// Ambient correlation id for audit attribution outside an HTTP request — e.g. a Hangfire
/// background job whose <c>SaveChanges</c> runs after the enqueueing request's
/// <c>Activity</c>/<c>HttpContext</c> are gone. AsyncLocal-backed, mirroring
/// <see cref="Neba.Api.Identity.AmbientActorContext"/>: the enqueueing endpoint captures the
/// request's correlation id and passes it into the job as a parameter (an AsyncLocal itself
/// cannot cross the Hangfire enqueue boundary), and the job sets it here for the duration of its
/// own async call chain so <c>AuditEnrichmentAction</c> can attribute the job's EF audit events
/// back to the request that triggered them.
/// </summary>
internal static class AmbientCorrelationContext
{
    private static readonly AsyncLocal<string?> Current = new();

    public static string? CorrelationId => Current.Value;

    /// <summary>
    /// Captures the correlation id for the currently executing request or activity, to be passed
    /// into a background job and restored there via <see cref="SetCorrelationId"/>.
    /// </summary>
    public static string Capture(HttpContext? httpContext) =>
        System.Diagnostics.Activity.Current?.TraceId.ToString()
        ?? httpContext?.TraceIdentifier
        ?? Guid.NewGuid().ToString();

    /// <summary>
    /// Sets the ambient correlation id for the remainder of the current async call chain. Dispose
    /// the returned scope when it no longer applies (e.g. at the end of a background job).
    /// </summary>
    public static IDisposable SetCorrelationId(string correlationId)
    {
        var previous = Current.Value;
        Current.Value = correlationId;
        return new CorrelationScope(previous);
    }

    private sealed class CorrelationScope(string? previous) : IDisposable
    {
        public void Dispose() => Current.Value = previous;
    }
}
