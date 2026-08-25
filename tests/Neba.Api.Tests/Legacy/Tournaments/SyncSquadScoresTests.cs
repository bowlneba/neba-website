using System.Net;
using System.Net.Http.Json;

using FluentValidation;

using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

using Neba.Api.Database;
using Neba.Api.Email;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Legacy;
using Neba.Api.Legacy.Bowlers;
using Neba.Api.Legacy.HallOfFame;
using Neba.Api.Legacy.Seasons.Complete;
using Neba.Api.Legacy.Tournaments;
using Neba.Api.Legacy.Tournaments.Complete;
using Neba.Api.Legacy.Tournaments.Stats;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

using Npgsql;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Legacy.Tournaments;

// Mirrors the single-file shape of the production source (Legacy/Tournaments/SyncSquadScores.cs) so
// the whole test suite for this backdoor action is removed alongside it at sunset with no leftover
// test files to hunt down.

[UnitTest]
[Component("Legacy")]
public sealed class SyncSquadScoresRequestValidatorTests
{
    private readonly SyncSquadScoresRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when SquadId is greater than zero")]
    public void Validate_ShouldSucceed_WhenSquadIdIsGreaterThanZero()
    {
        // Arrange
        var request = new SyncSquadScoresRequest(1);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail when SquadId is not greater than zero")]
    [InlineData(0, TestDisplayName = "Zero")]
    [InlineData(-1, TestDisplayName = "Negative")]
    public void Validate_ShouldFail_WhenSquadIdIsNotGreaterThanZero(int squadId)
    {
        // Arrange
        var request = new SyncSquadScoresRequest(squadId);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(SyncSquadScoresRequest.SquadId));
    }
}

[IntegrationTest]
[Component("Legacy")]
public sealed class SyncSquadScoresEndpointTests : IAsyncLifetime
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
        builder.Services.AddScoped<IValidator<SyncSquadScoresRequest>, SyncSquadScoresRequestValidator>();
        builder.Services.AddScoped<IValidator<CompleteTournamentRequest>, CompleteTournamentRequestValidator>();
        builder.Services.AddScoped<IValidator<UpdateTournamentStatsRequest>, UpdateTournamentStatsRequestValidator>();
        // Every sibling validator in the /legacy group is also required here: MapLegacyGroup() below
        // maps every endpoint in the group (not just this one), and ASP.NET Core builds route metadata
        // for the whole group on the first request to any of its endpoints - an unregistered
        // IValidator<T> for a sibling endpoint throws at that point, not just when that sibling is
        // called.
        builder.Services.AddScoped<IValidator<NewBowlerRequest>, NewBowlerRequestValidator>();
        builder.Services.AddScoped<IValidator<UpdateBowlerRequest>, UpdateBowlerRequestValidator>();
        builder.Services.AddScoped<IValidator<NewTournamentRequest>, NewTournamentRequestValidator>();
        builder.Services.AddScoped<IValidator<NewHallOfFameInductionRequest>, NewHallOfFameInductionRequestValidator>();
        builder.Services.AddScoped<IValidator<CompleteSeasonRequest>, CompleteSeasonRequestValidator>();
        builder.Services.AddSingleton(Options.Create(new LegacySettings { ApiKey = ValidApiKey }));

        _app = builder.Build();

        // Route through the real /legacy group (LegacyApiKeyFilter + MapLegacyEndpoints), not
        // MapSyncSquadScores() directly, so this test actually exercises the filter that protects the
        // route as deployed, and the relative path registered in SyncSquadScores.cs.
        _app.MapLegacyGroup();

        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    [Fact(DisplayName = "POST /legacy/squads/scores/sync returns 401 and does not enqueue a job when the X-Api-Key header is missing")]
    public async Task Post_ShouldReturn401AndNotEnqueue_WhenApiKeyHeaderIsMissing()
    {
        // Arrange
        using var client = _app.GetTestClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/squads/scores/sync",
            new SyncSquadScoresRequest(42),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /legacy/squads/scores/sync returns 401 and does not enqueue a job when the X-Api-Key header is wrong")]
    public async Task Post_ShouldReturn401AndNotEnqueue_WhenApiKeyHeaderIsWrong()
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/squads/scores/sync",
            new SyncSquadScoresRequest(42),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /legacy/squads/scores/sync returns 400 and does not enqueue a job when SquadId is invalid")]
    public async Task Post_ShouldReturn400AndNotEnqueue_WhenSquadIdIsInvalid()
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ValidApiKey);

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/squads/scores/sync",
            new SyncSquadScoresRequest(0),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /legacy/squads/scores/sync returns 202 and enqueues a SyncSquadScoresSyncJob with the request's SquadId")]
    public async Task Post_ShouldReturn202AndEnqueueSyncJob_WhenApiKeyAndSquadIdAreValid()
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
            "/legacy/squads/scores/sync",
            new SyncSquadScoresRequest(42),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        capturedJob.ShouldNotBeNull();
        capturedJob.Type.ShouldBe(typeof(SyncSquadScoresSyncJob));
        capturedJob.Method.Name.ShouldBe(nameof(SyncSquadScoresSyncJob.SyncAsync));
        capturedJob.Args[0].ShouldBe(42);
    }
}

[UnitTest]
[Component("Legacy")]
public sealed class UnsyncedBowlerScoreSyncEmailTests
{
    [Fact(DisplayName = "ToHtmlBody should include the legacy bowler id, legacy squad id, and score rows")]
    public void ToHtmlBody_ShouldIncludeLegacyBowlerIdLegacySquadIdAndScoreRows()
    {
        // Arrange
        var email = new UnsyncedBowlerScoreSyncEmail(
            legacyBowlerId: 42,
            legacySquadId: 7,
            unmappedRows: [new LegacyQualifyingScoreRow(42, 1, 200), new LegacyQualifyingScoreRow(42, 2, 210)]);

        // Act
        var body = email.ToHtmlBody();

        // Assert
        body.ShouldContain("42");
        body.ShouldContain("7");
        body.ShouldContain("200");
        body.ShouldContain("210");
    }
}

[IntegrationTest]
[Component("Legacy")]
[Collection<AppDbContextFixture>]
public sealed class SyncSquadScoresSyncJobTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private NpgsqlConnection _legacyConnection = null!;
    private ServiceProvider _serviceProvider = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();

        var services = new ServiceCollection();
        services.AddFusionCache().WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromHours(1));
        _serviceProvider = services.BuildServiceProvider();

        // Plain Dapper works against any real ADO.NET IDbConnection, so a Postgres connection
        // (reusing this test project's existing Testcontainers.PostgreSql infra) stands in for the
        // real MSSQL neba-fwk database here rather than standing up a second, MSSQL-specific
        // container for one temporary backdoor query. A CREATE TEMP TABLE is scoped to this single
        // connection - it's gone as soon as the connection closes, so it needs no cleanup and can't
        // collide with the shared fixture's own schema/Respawn reset.
        _legacyConnection = new NpgsqlConnection(fixture.ConnectionString);
        await _legacyConnection.OpenAsync();

        await using var createQualifyingScores = _legacyConnection.CreateCommand();
        createQualifyingScores.CommandText = """
            CREATE TEMP TABLE QualifyingScores (
                Id integer PRIMARY KEY,
                BowlerId integer NOT NULL,
                SquadId integer NOT NULL,
                Game integer NOT NULL,
                Score integer NOT NULL
            )
            """;
        await createQualifyingScores.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _legacyConnection.DisposeAsync();
        await _serviceProvider.DisposeAsync();
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private int _nextLegacyScoreId = 1;

    private async Task InsertLegacyScoreAsync(int legacyBowlerId, int legacySquadId, int game, int score)
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = "INSERT INTO QualifyingScores (Id, BowlerId, SquadId, Game, Score) VALUES (@Id, @BowlerId, @SquadId, @Game, @Score)";
        insert.Parameters.AddWithValue("@Id", _nextLegacyScoreId++);
        insert.Parameters.AddWithValue("@BowlerId", legacyBowlerId);
        insert.Parameters.AddWithValue("@SquadId", legacySquadId);
        insert.Parameters.AddWithValue("@Game", game);
        insert.Parameters.AddWithValue("@Score", score);
        await insert.ExecuteNonQueryAsync();
    }

    private async Task<Squad> CreateSquadAsync(int legacySquadId, CancellationToken ct)
    {
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        // TournamentFactory.Create's squads param feeds Tournament.AddSquad(...), which constructs a
        // brand-new Squad (its own Id) rather than persisting the passed-in instance - the actual
        // persisted squad must be read back off the tournament, not the SquadFactory.Create() input.
        var squadStub = SquadFactory.Create(legacyId: legacySquadId);
        var tournament = TournamentFactory.Create(seasonId: season.Id, squads: [squadStub]);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        return tournament.Squads.Single(s => s.LegacyId == legacySquadId);
    }

    private async Task<Bowler> CreateBowlerAsync(int legacyBowlerId, CancellationToken ct)
    {
        var bowler = BowlerFactory.Create(legacyId: legacyBowlerId);
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        return bowler;
    }

    private SyncSquadScoresSyncJob CreateJob(Mock<IEmailSender>? emailSender = null, FakeLogger<SyncSquadScoresSyncJob>? logger = null) =>
        new(
            _dbContext,
            _legacyConnection,
            _serviceProvider.GetRequiredService<IFusionCache>(),
            (emailSender ?? new Mock<IEmailSender>(MockBehavior.Strict)).Object,
            logger ?? new FakeLogger<SyncSquadScoresSyncJob>());

    [Fact(DisplayName = "SyncAsync should log a warning and make no changes when the legacy squad is not found")]
    public async Task SyncAsync_ShouldLogWarningAndMakeNoChanges_WhenLegacySquadIsNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fakeLogger = new FakeLogger<SyncSquadScoresSyncJob>();
        var job = CreateJob(logger: fakeLogger);

        // Act
        await job.SyncAsync(999, ct);

        // Assert - Strict email mock: any SendAsync call without a setup would throw.
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("999");
        (await _dbContext.SquadScores.ToListAsync(ct)).ShouldBeEmpty();
    }

    [Fact(DisplayName = "SyncAsync should create SquadScore rows with mapped BowlerId, GameNumber, and Score when all bowlers are mapped")]
    public async Task SyncAsync_ShouldCreateMappedSquadScoreRows_WhenAllBowlersAreMapped()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var squad = await CreateSquadAsync(legacySquadId: 1, ct);
        var bowler = await CreateBowlerAsync(legacyBowlerId: 100, ct);
        await InsertLegacyScoreAsync(legacyBowlerId: 100, legacySquadId: 1, game: 1, score: 200);
        await InsertLegacyScoreAsync(legacyBowlerId: 100, legacySquadId: 1, game: 2, score: 210);
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var scores = await _dbContext.SquadScores.Where(s => s.SquadId == squad.Id).OrderBy(s => s.GameNumber).ToListAsync(ct);
        scores.Count.ShouldBe(2);
        scores[0].BowlerId.ShouldBe(bowler.Id);
        scores[0].GameNumber.ShouldBe((short)1);
        scores[0].Score.ShouldBe(200);
        scores[1].GameNumber.ShouldBe((short)2);
        scores[1].Score.ShouldBe(210);
    }

    [Fact(DisplayName = "SyncAsync should evict the tournament's cache tag after saving")]
    public async Task SyncAsync_ShouldEvictTournamentCacheTag_AfterSaving()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var squad = await CreateSquadAsync(legacySquadId: 1, ct);
        await CreateBowlerAsync(legacyBowlerId: 100, ct);
        await InsertLegacyScoreAsync(legacyBowlerId: 100, legacySquadId: 1, game: 1, score: 200);

        var tournamentId = await _dbContext.Set<Tournament>()
            .Where(t => t.Squads.Any(s => s.Id == squad.Id))
            .Select(t => t.Id)
            .SingleAsync(ct);

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string cacheKey = "squad-scores-tournament-cache-test";
        await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("stale-cached-value"), tags: [$"neba:tournaments:{tournamentId}"], token: ct);

        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert - a stale cached value would be returned by GetOrSetAsync instead of invoking the factory.
        var valueAfterSync = await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("fresh-value"), token: ct);
        valueAfterSync.ShouldBe("fresh-value");
    }

    [Fact(DisplayName = "SyncAsync should skip an unmapped bowler's rows, sync others, log a warning, and send a manual-intervention email")]
    public async Task SyncAsync_ShouldSkipUnmappedBowlerAndSendEmail_WhenOneLegacyBowlerIdHasNoMatch()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var squad = await CreateSquadAsync(legacySquadId: 1, ct);
        var mappedBowler = await CreateBowlerAsync(legacyBowlerId: 100, ct);
        await InsertLegacyScoreAsync(legacyBowlerId: 100, legacySquadId: 1, game: 1, score: 200);
        await InsertLegacyScoreAsync(legacyBowlerId: 999, legacySquadId: 1, game: 1, score: 150);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        EmailMessage? sentMessage = null;
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => sentMessage = message)
            .Returns(Task.CompletedTask);

        var fakeLogger = new FakeLogger<SyncSquadScoresSyncJob>();
        var job = CreateJob(emailSender, fakeLogger);

        // Act
        await job.SyncAsync(1, ct);

        // Assert - Strict mock: the Setup above is the verification that SendAsync was called.
        var scores = await _dbContext.SquadScores.Where(s => s.SquadId == squad.Id).ToListAsync(ct);
        var mappedScore = scores.ShouldHaveSingleItem();
        mappedScore.BowlerId.ShouldBe(mappedBowler.Id);

        sentMessage.ShouldNotBeNull();
        sentMessage.To.ShouldBe("website@bowlneba.com");
        sentMessage.HtmlBody.ShouldContain("999");
        sentMessage.HtmlBody.ShouldContain("150");

        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Warning && r.Message.Contains("999", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "SyncAsync should send two separate emails, one per unmapped bowler, each scoped to only that bowler's rows")]
    public async Task SyncAsync_ShouldSendSeparateEmailsPerUnmappedBowler_WhenTwoLegacyBowlerIdsAreUnmapped()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await CreateSquadAsync(legacySquadId: 1, ct);
        await InsertLegacyScoreAsync(legacyBowlerId: 901, legacySquadId: 1, game: 1, score: 111);
        await InsertLegacyScoreAsync(legacyBowlerId: 902, legacySquadId: 1, game: 1, score: 222);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        var sentMessages = new List<EmailMessage>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => sentMessages.Add(message))
            .Returns(Task.CompletedTask);
        var job = CreateJob(emailSender);

        // Act
        await job.SyncAsync(1, ct);

        // Assert - Strict mock: the Setup above is the verification that SendAsync was called twice.
        sentMessages.Count.ShouldBe(2);
        sentMessages.ShouldContain(m => m.HtmlBody.Contains("901", StringComparison.Ordinal) && m.HtmlBody.Contains("111", StringComparison.Ordinal) && !m.HtmlBody.Contains("902", StringComparison.Ordinal));
        sentMessages.ShouldContain(m => m.HtmlBody.Contains("902", StringComparison.Ordinal) && m.HtmlBody.Contains("222", StringComparison.Ordinal) && !m.HtmlBody.Contains("901", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "SyncAsync should not send an email when all bowlers are mapped")]
    public async Task SyncAsync_ShouldNotSendEmail_WhenAllBowlersAreMapped()
    {
        // Arrange - Strict mock with no setup: any SendAsync call would throw.
        var ct = TestContext.Current.CancellationToken;
        await CreateSquadAsync(legacySquadId: 1, ct);
        await CreateBowlerAsync(legacyBowlerId: 100, ct);
        await InsertLegacyScoreAsync(legacyBowlerId: 100, legacySquadId: 1, game: 1, score: 200);
        var job = CreateJob();

        // Act & Assert
        await Should.NotThrowAsync(() => job.SyncAsync(1, ct));
    }

    [Fact(DisplayName = "SyncAsync should remove existing squad_scores rows for bowlers/games no longer present in the legacy result set")]
    public async Task SyncAsync_ShouldRemoveStaleRows_WhenNoLongerPresentInLegacyResultSet()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var squad = await CreateSquadAsync(legacySquadId: 1, ct);
        var staleBowler = await CreateBowlerAsync(legacyBowlerId: 100, ct);
        var staleScore = SquadScoreFactory.Create(squadId: squad.Id, bowlerId: staleBowler.Id, gameNumber: 1, value: 150);
        await _dbContext.SquadScores.AddAsync(staleScore, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        var freshBowler = await CreateBowlerAsync(legacyBowlerId: 200, ct);
        await InsertLegacyScoreAsync(legacyBowlerId: 200, legacySquadId: 1, game: 1, score: 250);
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert - full replace: the stale row for the bowler no longer in the legacy payload is gone.
        var scores = await _dbContext.SquadScores.Where(s => s.SquadId == squad.Id).ToListAsync(ct);
        var remaining = scores.ShouldHaveSingleItem();
        remaining.BowlerId.ShouldBe(freshBowler.Id);
    }

    [Fact(DisplayName = "SyncAsync should skip a row with an out-of-range score and log an error rather than throwing")]
    public async Task SyncAsync_ShouldSkipAndLogError_WhenScoreIsOutOfRange()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var squad = await CreateSquadAsync(legacySquadId: 1, ct);
        await CreateBowlerAsync(legacyBowlerId: 100, ct);
        await InsertLegacyScoreAsync(legacyBowlerId: 100, legacySquadId: 1, game: 1, score: 301);
        var fakeLogger = new FakeLogger<SyncSquadScoresSyncJob>();
        var job = CreateJob(logger: fakeLogger);

        // Act
        await Should.NotThrowAsync(() => job.SyncAsync(1, ct));

        // Assert
        (await _dbContext.SquadScores.Where(s => s.SquadId == squad.Id).ToListAsync(ct)).ShouldBeEmpty();
        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Error);
    }

    [Fact(DisplayName = "SyncAsync should query only the rows for the requested legacy SquadId")]
    public async Task SyncAsync_ShouldQueryOnlyRowsForRequestedSquadId()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var squad = await CreateSquadAsync(legacySquadId: 1, ct);
        var otherSquad = await CreateSquadAsync(legacySquadId: 2, ct);
        await CreateBowlerAsync(legacyBowlerId: 100, ct);
        await InsertLegacyScoreAsync(legacyBowlerId: 100, legacySquadId: 1, game: 1, score: 200);
        await InsertLegacyScoreAsync(legacyBowlerId: 100, legacySquadId: 2, game: 1, score: 275);
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var squadOneScores = await _dbContext.SquadScores.Where(s => s.SquadId == squad.Id).ToListAsync(ct);
        var score = squadOneScores.ShouldHaveSingleItem();
        score.Score.ShouldBe(200);
        (await _dbContext.SquadScores.Where(s => s.SquadId == otherSquad.Id).ToListAsync(ct)).ShouldBeEmpty();
    }

    [Fact(DisplayName = "SyncAsync should converge to the same row set when run twice for the same legacy squad id")]
    public async Task SyncAsync_ShouldConvergeToSameRowSet_WhenRunTwice()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var squad = await CreateSquadAsync(legacySquadId: 1, ct);
        var bowler = await CreateBowlerAsync(legacyBowlerId: 100, ct);
        await InsertLegacyScoreAsync(legacyBowlerId: 100, legacySquadId: 1, game: 1, score: 200);
        await InsertLegacyScoreAsync(legacyBowlerId: 100, legacySquadId: 1, game: 2, score: 210);
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);
        _dbContext.ChangeTracker.Clear();
        await job.SyncAsync(1, ct);

        // Assert
        var scores = await _dbContext.SquadScores.Where(s => s.SquadId == squad.Id).OrderBy(s => s.GameNumber).ToListAsync(ct);
        scores.Count.ShouldBe(2);
        scores[0].BowlerId.ShouldBe(bowler.Id);
        scores[0].Score.ShouldBe(200);
        scores[1].Score.ShouldBe(210);
    }
}