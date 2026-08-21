using System.Net;
using System.Net.Http.Json;

using FluentValidation;

using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Neba.Api.Legacy;
using Neba.Api.Legacy.Bowlers;
using Neba.Api.Legacy.HallOfFame;
using Neba.Api.Legacy.Tournaments;
using Neba.Api.Legacy.Tournaments.Complete;
using Neba.Api.Legacy.Tournaments.Stats;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy.Tournaments.Stats;

[IntegrationTest]
[Component("Legacy")]
public sealed class UpdateTournamentStatsEndpointTests : IAsyncLifetime
{
    private const string ValidApiKey = "test-legacy-api-key";

    private WebApplication _app = null!;
    private Mock<IBackgroundJobClient> _jobsMock = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        _jobsMock = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        builder.Services.AddSingleton(_jobsMock.Object);
        builder.Services.AddScoped<IValidator<UpdateTournamentStatsRequest>, UpdateTournamentStatsRequestValidator>();
        // Every sibling validator in the /legacy group is also required here: MapLegacyGroup() below
        // maps every endpoint in the group (not just this one), and ASP.NET Core builds route metadata
        // for the whole group on the first request to any of its endpoints - an unregistered
        // IValidator<T> for a sibling endpoint throws at that point, not just when that sibling is called.
        builder.Services.AddScoped<IValidator<NewBowlerRequest>, NewBowlerRequestValidator>();
        builder.Services.AddScoped<IValidator<UpdateBowlerRequest>, UpdateBowlerRequestValidator>();
        builder.Services.AddScoped<IValidator<NewTournamentRequest>, NewTournamentRequestValidator>();
        builder.Services.AddScoped<IValidator<SyncSquadScoresRequest>, SyncSquadScoresRequestValidator>();
        builder.Services.AddScoped<IValidator<CompleteTournamentRequest>, CompleteTournamentRequestValidator>();
        builder.Services.AddScoped<IValidator<NewHallOfFameInductionRequest>, NewHallOfFameInductionRequestValidator>();
        builder.Services.AddSingleton(Options.Create(new LegacySettings { ApiKey = ValidApiKey }));

        _app = builder.Build();

        // Route through the real /legacy group (LegacyApiKeyFilter + MapLegacyEndpoints), not
        // MapUpdateTournamentStats() directly, so this test actually exercises the filter that
        // protects the route as deployed, and the relative path registered in the endpoint file.
        _app.MapLegacyGroup();

        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    [Fact(DisplayName = "POST /legacy/tournaments/stats/update returns 401 and does not enqueue a job when the X-Api-Key header is missing")]
    public async Task Post_ShouldReturn401AndNotEnqueue_WhenApiKeyHeaderIsMissing()
    {
        // Arrange
        using var client = _app.GetTestClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/tournaments/stats/update",
            new UpdateTournamentStatsRequest(42),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /legacy/tournaments/stats/update returns 401 and does not enqueue a job when the X-Api-Key header is wrong")]
    public async Task Post_ShouldReturn401AndNotEnqueue_WhenApiKeyHeaderIsWrong()
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/tournaments/stats/update",
            new UpdateTournamentStatsRequest(42),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /legacy/tournaments/stats/update returns 400 and does not enqueue a job when TournamentId is invalid")]
    public async Task Post_ShouldReturn400AndNotEnqueue_WhenTournamentIdIsInvalid()
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ValidApiKey);

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/tournaments/stats/update",
            new UpdateTournamentStatsRequest(0),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /legacy/tournaments/stats/update returns 202 and enqueues a GenerateSeasonStatsJob with the request's TournamentId")]
    public async Task Post_ShouldReturn202AndEnqueueSyncJob_WhenApiKeyAndTournamentIdAreValid()
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
        using var response = await client.PostAsJsonAsync(
            "/legacy/tournaments/stats/update",
            new UpdateTournamentStatsRequest(42),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        capturedJob.ShouldNotBeNull();
        capturedJob.Type.ShouldBe(typeof(GenerateSeasonStatsJob));
        capturedJob.Method.Name.ShouldBe(nameof(GenerateSeasonStatsJob.SyncAsync));
        capturedJob.Args[0].ShouldBe(42);
    }
}