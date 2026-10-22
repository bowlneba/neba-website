namespace Neba.Website.Server.Services;

/// <summary>
/// Exposes the current Blazor circuit's DI scope to code that runs in a different scope, such as
/// <see cref="BearerTokenHandler"/>, which <c>IHttpClientFactory</c> builds in its own scope and
/// shares across users. Null outside circuit activity. Set by <see cref="CircuitServicesActivityHandler"/>.
/// </summary>
/// <remarks>
/// See "Access server-side Blazor services from a different DI scope" in the ASP.NET Core docs.
/// </remarks>
internal sealed class CircuitServicesAccessor
{
    private readonly AsyncLocal<IServiceProvider?> _services = new();

    public IServiceProvider? Services
    {
        get => _services.Value;
        set => _services.Value = value;
    }
}