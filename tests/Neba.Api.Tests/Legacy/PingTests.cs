using System.Net;

using FluentValidation;

using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

using Neba.Api.Legacy;
using Neba.Api.Legacy.Bowlers;
using Neba.Api.Legacy.HallOfFame;
using Neba.Api.Legacy.Seasons.Complete;
using Neba.Api.Legacy.Tournaments;
using Neba.Api.Legacy.Tournaments.Complete;
using Neba.Api.Legacy.Tournaments.Stats;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy;

// Mirrors the single-file shape of the production source (Legacy/Ping.cs) so the whole test suite
// for this backdoor action is removed alongside it at sunset with no leftover test files to hunt down.

[IntegrationTest]
[Component("Legacy")]
public sealed class PingEndpointTests : IAsyncLifetime
{
    private const string ValidApiKey = "test-legacy-api-key";
    private static readonly Uri PingUri = new("/legacy/ping", UriKind.Relative);

    private WebApplication _app = null!;
    private Mock<IBackgroundJobClient> _jobsMock = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        _jobsMock = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        builder.Services.AddSingleton(_jobsMock.Object);

        // MapLegacyGroup() below maps every endpoint in the /legacy group (not just this one), and
        // ASP.NET Core builds route metadata for the whole group on the first request to any of its
        // endpoints - an unregistered IValidator<T> for a sibling endpoint throws at that point, not
        // just when Ping is called.
        builder.Services.AddScoped<IValidator<NewBowlerRequest>, NewBowlerRequestValidator>();
        builder.Services.AddScoped<IValidator<UpdateBowlerRequest>, UpdateBowlerRequestValidator>();
        builder.Services.AddScoped<IValidator<NewTournamentRequest>, NewTournamentRequestValidator>();
        builder.Services.AddScoped<IValidator<SyncSquadScoresRequest>, SyncSquadScoresRequestValidator>();
        builder.Services.AddScoped<IValidator<CompleteTournamentRequest>, CompleteTournamentRequestValidator>();
        builder.Services.AddScoped<IValidator<UpdateTournamentStatsRequest>, UpdateTournamentStatsRequestValidator>();
        builder.Services.AddScoped<IValidator<NewHallOfFameInductionRequest>, NewHallOfFameInductionRequestValidator>();
        builder.Services.AddScoped<IValidator<CompleteSeasonRequest>, CompleteSeasonRequestValidator>();
        builder.Services.AddSingleton(Options.Create(new LegacySettings { ApiKey = ValidApiKey }));

        _app = builder.Build();

        // Route through the real /legacy group (LegacyApiKeyFilter + MapLegacyEndpoints), not
        // MapPing() directly, so this test actually exercises the filter that protects the route
        // as deployed.
        _app.MapLegacyGroup();

        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    [Fact(DisplayName = "POST /legacy/ping returns 401 and does not enqueue a job when the X-Api-Key header is missing")]
    public async Task Post_ShouldReturn401AndNotEnqueue_WhenApiKeyHeaderIsMissing()
    {
        // Arrange
        using var client = _app.GetTestClient();

        // Act
        using var response = await client.PostAsync(PingUri, content: null, TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /legacy/ping returns 401 and does not enqueue a job when the X-Api-Key header is wrong")]
    public async Task Post_ShouldReturn401AndNotEnqueue_WhenApiKeyHeaderIsWrong()
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        // Act
        using var response = await client.PostAsync(PingUri, content: null, TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /legacy/ping returns 202 and enqueues a PongJob when the API key is valid")]
    public async Task Post_ShouldReturn202AndEnqueuePongJob_WhenApiKeyIsValid()
    {
        // Arrange
        Job? capturedJob = null;
        _jobsMock
            .Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, _) => capturedJob = job)
            .Returns("job-1");

        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ValidApiKey);

        // Act
        using var response = await client.PostAsync(PingUri, content: null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        capturedJob.ShouldNotBeNull();
        capturedJob.Type.ShouldBe(typeof(PongJob));
        capturedJob.Method.Name.ShouldBe(nameof(PongJob.PongAsync));
    }
}

[UnitTest]
[Component("Legacy")]
public sealed class PongJobTests
{
    private static PongJob CreateJob(
        IHttpClientFactory httpClientFactory,
        IServer server,
        FakeLogger<PongJob>? logger = null) =>
        new(httpClientFactory, server, logger ?? new FakeLogger<PongJob>());

    private static IServer CreateServer(params string[] addresses)
    {
        var features = new FeatureCollection();

        if (addresses.Length > 0)
        {
            var addressesFeature = new ServerAddressesFeature();
            foreach (var address in addresses)
            {
                addressesFeature.Addresses.Add(address);
            }

            features.Set<IServerAddressesFeature>(addressesFeature);
        }

        var serverMock = new Mock<IServer>(MockBehavior.Strict);
        serverMock.Setup(s => s.Features).Returns(features);

        return serverMock.Object;
    }

    [Fact(DisplayName = "PongAsync should log the status code and body when the health check succeeds")]
    public async Task PongAsync_ShouldLogStatusCodeAndBody_WhenHealthCheckSucceeds()
    {
        // Arrange
        var fakeLogger = new FakeLogger<PongJob>();
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "Healthy");
        var job = CreateJob(new FakeHttpClientFactory(handler), CreateServer("http://localhost:5000"), fakeLogger);

        // Act
        await job.PongAsync(TestContext.Current.CancellationToken);

        // Assert
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldContain("200");
        record.Message.ShouldContain("Healthy");
    }

    [Fact(DisplayName = "PongAsync should log a warning and rethrow when the health check request throws HttpRequestException")]
    public async Task PongAsync_ShouldLogWarningAndRethrow_WhenHealthCheckThrowsHttpRequestException()
    {
        // Arrange
        var fakeLogger = new FakeLogger<PongJob>();
        using var handler = new ThrowingHttpMessageHandler(new HttpRequestException("connection refused"));
        var job = CreateJob(new FakeHttpClientFactory(handler), CreateServer("http://localhost:5000"), fakeLogger);

        // Act
        await Should.ThrowAsync<HttpRequestException>(() => job.PongAsync(TestContext.Current.CancellationToken));

        // Assert
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("connection refused");
    }

    [Fact(DisplayName = "PongAsync should log a warning and rethrow when the health check request times out")]
    public async Task PongAsync_ShouldLogWarningAndRethrow_WhenHealthCheckTimesOut()
    {
        // Arrange
        var fakeLogger = new FakeLogger<PongJob>();
        using var handler = new ThrowingHttpMessageHandler(new TaskCanceledException("timed out"));
        var job = CreateJob(new FakeHttpClientFactory(handler), CreateServer("http://localhost:5000"), fakeLogger);

        // Act
        await Should.ThrowAsync<TaskCanceledException>(() => job.PongAsync(TestContext.Current.CancellationToken));

        // Assert
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("timed out");
    }

    [Fact(DisplayName = "PongAsync should log the status code and body then throw when the health check returns a non-success status code")]
    public async Task PongAsync_ShouldLogAndThrow_WhenHealthCheckReturnsNonSuccessStatusCode()
    {
        // Arrange
        var fakeLogger = new FakeLogger<PongJob>();
        using var handler = new StubHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "Unhealthy");
        var job = CreateJob(new FakeHttpClientFactory(handler), CreateServer("http://localhost:5000"), fakeLogger);

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => job.PongAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldContain("503");
        exception.Message.ShouldContain("Unhealthy");
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldContain("503");
        record.Message.ShouldContain("Unhealthy");
    }

    [Fact(DisplayName = "PongAsync should log a warning and not call the HTTP client when no server address is available")]
    public async Task PongAsync_ShouldLogWarningAndNotCallHttpClient_WhenNoServerAddressIsAvailable()
    {
        // Arrange
        var fakeLogger = new FakeLogger<PongJob>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var job = CreateJob(httpClientFactoryMock.Object, CreateServer(), fakeLogger);

        // Act
        await job.PongAsync(TestContext.Current.CancellationToken);

        // Assert - Strict mock: any CreateClient call without a setup would throw, proving no HTTP call was attempted.
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("could not resolve");
    }

    [Fact(DisplayName = "PongAsync should log a warning and not call the HTTP client when the addresses collection is empty")]
    public async Task PongAsync_ShouldLogWarningAndNotCallHttpClient_WhenAddressesCollectionIsEmpty()
    {
        // Arrange
        var fakeLogger = new FakeLogger<PongJob>();
        var features = new FeatureCollection();
        features.Set<IServerAddressesFeature>(new ServerAddressesFeature());
        var serverMock = new Mock<IServer>(MockBehavior.Strict);
        serverMock.Setup(s => s.Features).Returns(features);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        var job = CreateJob(httpClientFactoryMock.Object, serverMock.Object, fakeLogger);

        // Act
        await job.PongAsync(TestContext.Current.CancellationToken);

        // Assert - Strict mock: any CreateClient call without a setup would throw, proving no HTTP call was attempted.
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("could not resolve");
    }

    [Fact(DisplayName = "PongAsync should not throw and should log twice when called twice in a row")]
    public async Task PongAsync_ShouldNotThrowAndShouldLogTwice_WhenCalledTwiceInARow()
    {
        // Arrange
        var fakeLogger = new FakeLogger<PongJob>();
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "Healthy");
        var job = CreateJob(new FakeHttpClientFactory(handler), CreateServer("http://localhost:5000"), fakeLogger);

        // Act
        await job.PongAsync(TestContext.Current.CancellationToken);
        await job.PongAsync(TestContext.Current.CancellationToken);

        // Assert
        fakeLogger.Collector.GetSnapshot().Count.ShouldBe(2);
    }

    [Theory(DisplayName = "PongAsync should normalize wildcard bind addresses to localhost before calling /health")]
    [InlineData("http://+:8080", "http://localhost:8080/health")]
    [InlineData("http://*:8080", "http://localhost:8080/health")]
    [InlineData("http://[::]:8080", "http://localhost:8080/health")]
    [InlineData("http://0.0.0.0:8080", "http://localhost:8080/health")]
    [InlineData("http://localhost:5000", "http://localhost:5000/health")]
    public async Task PongAsync_ShouldNormalizeWildcardBindAddress_BeforeCallingHealth(string boundAddress, string expectedRequestTarget)
    {
        // Arrange
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "OK");
        var job = CreateJob(new FakeHttpClientFactory(handler), CreateServer(boundAddress));

        // Act
        await job.PongAsync(TestContext.Current.CancellationToken);

        // Assert
        handler.RequestedUri.ShouldNotBeNull();
        handler.RequestedUri.ToString().ShouldBe(expectedRequestTarget);
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        public Uri? RequestedUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(body) });
        }
    }

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw exception;
    }
}
