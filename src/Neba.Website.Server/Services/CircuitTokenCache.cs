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
}
