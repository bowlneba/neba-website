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
using Neba.Api.Legacy.Seasons.Complete;
using Neba.Api.Legacy.Tournaments;
using Neba.Api.Legacy.Tournaments.Complete;
using Neba.Api.Legacy.Tournaments.Stats;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy.Tournaments.Complete;

[IntegrationTest]
[Component("Legacy")]
public sealed class SyncTournamentResultsEndpointTests : IAsyncLifetime
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
        builder.Services.AddScoped<IValidator<CompleteTournamentRequest>, CompleteTournamentRequestValidator>();
        // Every sibling validator in the /legacy group is also required here; see
        // CompleteTournamentEndpointTests for why.
        builder.Services.AddScoped<IValidator<UpdateTournamentStatsRequest>, UpdateTournamentStatsRequestValidator>();
        builder.Services.AddScoped<IValidator<NewBowlerRequest>, NewBowlerRequestValidator>();
        builder.Services.AddScoped<IValidator<UpdateBowlerRequest>, UpdateBowlerRequestValidator>();
        builder.Services.AddScoped<IValidator<NewTournamentRequest>, NewTournamentRequestValidator>();
        builder.Services.AddScoped<IValidator<SyncSquadScoresRequest>, SyncSquadScoresRequestValidator>();
        builder.Services.AddScoped<IValidator<NewHallOfFameInductionRequest>, NewHallOfFameInductionRequestValidator>();
        builder.Services.AddScoped<IValidator<CompleteSeasonRequest>, CompleteSeasonRequestValidator>();
        builder.Services.AddSingleton(Options.Create(new LegacySettings { ApiKey = ValidApiKey }));

        _app = builder.Build();

        _app.MapLegacyGroup();

        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    [Fact(DisplayName = "POST /legacy/tournaments/results/sync returns 401 and does not enqueue a job when the X-Api-Key header is missing")]
    public async Task Post_ShouldReturn401AndNotEnqueue_WhenApiKeyHeaderIsMissing()
    {
        // Arrange
        using var client = _app.GetTestClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/tournaments/results/sync",
            new CompleteTournamentRequest(42),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /legacy/tournaments/results/sync returns 400 and does not enqueue a job when TournamentId is invalid")]
    public async Task Post_ShouldReturn400AndNotEnqueue_WhenTournamentIdIsInvalid()
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ValidApiKey);

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/tournaments/results/sync",
            new CompleteTournamentRequest(0),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /legacy/tournaments/results/sync returns 202 and enqueues only a SyncTournamentResultsJob with the request's TournamentId")]
    public async Task Post_ShouldReturn202AndEnqueueSyncJob_WhenApiKeyAndTournamentIdAreValid()
    {
        // Arrange
        List<Job> capturedJobs = [];
        _jobsMock
            .Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, _) => capturedJobs.Add(job))
            .Returns("job-1");

        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ValidApiKey);

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/tournaments/results/sync",
            new CompleteTournamentRequest(42),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var job = capturedJobs.ShouldHaveSingleItem();
        job.Type.ShouldBe(typeof(SyncTournamentResultsJob));
        job.Method.Name.ShouldBe(nameof(SyncTournamentResultsJob.SyncAsync));
        job.Args[0].ShouldBe(42);
    }
}