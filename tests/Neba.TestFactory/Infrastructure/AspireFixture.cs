using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Neba.TestFactory.Infrastructure;

/// <summary>
/// Shared fixture for integration tests that spins up the full Aspire AppHost.
/// Use with [Collection&lt;AspireFixture&gt;] to share across test classes.
/// </summary>
public sealed class AspireFixture : IAsyncLifetime
{
    private DistributedApplication? _app;

    /// <summary>
    /// HTTP client configured to communicate with the API resource.
    /// </summary>
    public HttpClient ApiClient { get; private set; } = null!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Neba_AppHost>();

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("api")
            .WaitAsync(TimeSpan.FromSeconds(30));

        ApiClient = _app.CreateHttpClient("api");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}