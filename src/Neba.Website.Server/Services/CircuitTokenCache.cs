namespace Neba.Website.Server.Services;

/// <summary>
/// Caches the current circuit's access/refresh tokens for <see cref="BearerTokenHandler"/> to fall
/// back on when <see cref="IHttpContextAccessor.HttpContext"/> is unavailable — which happens for
/// any API call made purely over the SignalR circuit (e.g. from a component rendered with
/// <c>prerender: false</c>), since <c>HttpContext</c> is only populated for the duration of an
/// actual HTTP request, not later circuit-driven interactions. Seeded once per circuit by
/// <c>Routes.razor</c>, the last point in the render tree where a real <c>HttpContext</c> is
/// reliably available. Registered as scoped, matching Blazor Server's one-DI-scope-per-circuit
/// lifetime.
/// </summary>
internal sealed class CircuitTokenCache
{
    public string? UserId { get; set; }

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    /// <summary>
    /// Serializes refresh attempts within this circuit. <see cref="BearerTokenHandler"/> is
    /// transient - a burst of concurrent outgoing calls (e.g. several near-simultaneous uploads)
    /// each get their own instance with no shared state, so without this lock each one would
    /// independently race the others to POST /security/refresh with the same still-current refresh
    /// token; only the first to land rotates it, and the rest get InvalidRefreshToken. Not disposed
    /// (SemaphoreSlim(1,1) holds no unmanaged resource worth tracking through this scoped
    /// service's lifetime) - deliberately not IDisposable to avoid forcing every call site that
    /// constructs one to dispose it too.
    /// </summary>
    public SemaphoreSlim RefreshLock { get; } = new(1, 1);
}