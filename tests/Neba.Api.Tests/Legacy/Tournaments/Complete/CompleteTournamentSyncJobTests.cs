using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Database;
using Neba.Api.Email;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Legacy.Tournaments.Complete;
using Neba.Api.Legacy.Tournaments.Stats;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Legacy.Tournaments.Complete;

// This job's whole job is "complete, then chain" - covered separately from the endpoint test
// (which only proves CompleteTournamentSyncJob gets enqueued) and from SyncTournamentResultsJob
// (which does the actual results work once chained here).
[IntegrationTest]
[Component("Legacy")]
[Collection<AppDbContextFixture>]
public sealed class CompleteTournamentSyncJobTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync() => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task<Tournament> CreateTournamentAsync(int legacyTournamentId, CancellationToken ct)
    {
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var tournament = TournamentFactory.Create(legacyId: legacyTournamentId, seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        return tournament;
    }

    private CompleteTournamentSyncJob CreateJob(
        IBackgroundJobClient jobs,
        Mock<IEmailSender>? emailSender = null,
        FakeLogger<CompleteTournamentSyncJob>? logger = null) =>
        new(_dbContext, jobs, (emailSender ?? new Mock<IEmailSender>(MockBehavior.Strict)).Object, logger ?? new FakeLogger<CompleteTournamentSyncJob>());

    private static (Mock<IBackgroundJobClient> Mock, Func<IReadOnlyList<(Job Job, IState State)>> CapturedJobs) CreateJobsMock()
    {
        var captured = new List<(Job Job, IState State)>();
        var mock = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        mock.Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, state) => captured.Add((job, state)))
            .Returns("job-1");

        return (mock, () => captured);
    }

    [Fact(DisplayName = "SyncAsync should complete the tournament, save, enqueue SyncTournamentResultsJob, and schedule GenerateSeasonStatsJob ten minutes out when the tournament is found and not yet complete")]
    public async Task SyncAsync_ShouldCompleteSaveAndChain_WhenTournamentFoundAndNotComplete()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await CreateTournamentAsync(legacyTournamentId: 42, ct);
        var (jobsMock, capturedJobs) = CreateJobsMock();
        var job = CreateJob(jobsMock.Object);

        // Act
        await job.SyncAsync(42, ct);

        // Assert
        var tournament = await _dbContext.Tournaments.SingleAsync(t => t.LegacyId == 42, ct);
        tournament.Complete.ShouldBeTrue();

        var chained = capturedJobs();
        chained.Count.ShouldBe(2);

        var resultsJob = chained.Single(c => c.Job.Type == typeof(SyncTournamentResultsJob));
        resultsJob.Job.Method.Name.ShouldBe(nameof(SyncTournamentResultsJob.SyncAsync));
        resultsJob.Job.Args[0].ShouldBe(42);
        resultsJob.State.ShouldBeOfType<EnqueuedState>();

        var statsJob = chained.Single(c => c.Job.Type == typeof(GenerateSeasonStatsJob));
        statsJob.Job.Method.Name.ShouldBe(nameof(GenerateSeasonStatsJob.SyncAsync));
        statsJob.Job.Args[0].ShouldBe(42);
        var scheduledState = statsJob.State.ShouldBeOfType<ScheduledState>();
        scheduledState.EnqueueAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(9));
    }

    [Fact(DisplayName = "SyncAsync should still chain SyncTournamentResultsJob and GenerateSeasonStatsJob when the tournament was already complete")]
    public async Task SyncAsync_ShouldStillChain_WhenTournamentAlreadyComplete()
    {
        // Arrange - idempotent re-fire: AlreadyComplete is informational, not fatal.
        var ct = TestContext.Current.CancellationToken;
        await CreateTournamentAsync(legacyTournamentId: 42, ct);

        // CreateTournamentAsync clears the change tracker before returning, so its result is
        // detached - reload before completing, or the completion never gets tracked/persisted.
        var tracked = await _dbContext.Tournaments.SingleAsync(t => t.LegacyId == 42, ct);
        tracked.CompleteTournament();
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        var (jobsMock, capturedJobs) = CreateJobsMock();
        var fakeLogger = new FakeLogger<CompleteTournamentSyncJob>();
        var job = CreateJob(jobsMock.Object, logger: fakeLogger);

        // Act
        await job.SyncAsync(42, ct);

        // Assert
        var chained = capturedJobs();
        chained.ShouldContain(c => c.Job.Type == typeof(SyncTournamentResultsJob));
        chained.ShouldContain(c => c.Job.Type == typeof(GenerateSeasonStatsJob));
        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Information);
    }

    [Fact(DisplayName = "SyncAsync should not chain SyncTournamentResultsJob and should send a manual-intervention email when no tournament is linked")]
    public async Task SyncAsync_ShouldNotChainAndShouldSendEmail_WhenTournamentNotFound()
    {
        // Arrange - Strict jobs mock with no setup: any Create call would throw, proving the chain does not fire.
        var ct = TestContext.Current.CancellationToken;
        var jobsMock = new Mock<IBackgroundJobClient>(MockBehavior.Strict);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        EmailMessage? sentMessage = null;
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => sentMessage = message)
            .Returns(Task.CompletedTask);

        var job = CreateJob(jobsMock.Object, emailSender);

        // Act
        await job.SyncAsync(999, ct);

        // Assert - Strict mock with no Create setup already proves the chain didn't fire (see Arrange comment).
        sentMessage.ShouldNotBeNull();
        sentMessage.To.ShouldBe("website@bowlneba.com");
        sentMessage.HtmlBody.ShouldContain("999");
    }
}