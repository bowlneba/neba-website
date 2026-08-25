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
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

using Npgsql;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Legacy.Tournaments;

// Mirrors the single-file shape of the production source (Legacy/Tournaments/NewTournament.cs) so
// the whole test suite for this backdoor action is removed alongside it at sunset with no leftover
// test files to hunt down.

[UnitTest]
[Component("Legacy")]
public sealed class NewTournamentRequestValidatorTests
{
    private readonly NewTournamentRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when TournamentId is greater than zero")]
    public void Validate_ShouldSucceed_WhenTournamentIdIsGreaterThanZero()
    {
        // Arrange
        var request = new NewTournamentRequest(1);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail when TournamentId is not greater than zero")]
    [InlineData(0, TestDisplayName = "Zero")]
    [InlineData(-1, TestDisplayName = "Negative")]
    public void Validate_ShouldFail_WhenTournamentIdIsNotGreaterThanZero(int tournamentId)
    {
        // Arrange
        var request = new NewTournamentRequest(tournamentId);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NewTournamentRequest.TournamentId));
    }
}

[IntegrationTest]
[Component("Legacy")]
public sealed class NewTournamentEndpointTests : IAsyncLifetime
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
        builder.Services.AddScoped<IValidator<NewTournamentRequest>, NewTournamentRequestValidator>();
        // NewBowlerRequest's/UpdateBowlerRequest's/SyncSquadScoresRequest's validators are also
        // required here: MapLegacyGroup() below maps every endpoint in the /legacy group (not just
        // this one), and ASP.NET Core builds route metadata for the whole group on the first request
        // to any of its endpoints - an unregistered IValidator<T> for a sibling endpoint throws at
        // that point, not just when that sibling is called.
        builder.Services.AddScoped<IValidator<NewBowlerRequest>, NewBowlerRequestValidator>();
        builder.Services.AddScoped<IValidator<UpdateBowlerRequest>, UpdateBowlerRequestValidator>();
        builder.Services.AddScoped<IValidator<SyncSquadScoresRequest>, SyncSquadScoresRequestValidator>();
        builder.Services.AddScoped<IValidator<CompleteTournamentRequest>, CompleteTournamentRequestValidator>();
        builder.Services.AddScoped<IValidator<UpdateTournamentStatsRequest>, UpdateTournamentStatsRequestValidator>();
        builder.Services.AddScoped<IValidator<NewHallOfFameInductionRequest>, NewHallOfFameInductionRequestValidator>();
        builder.Services.AddScoped<IValidator<CompleteSeasonRequest>, CompleteSeasonRequestValidator>();
        builder.Services.AddSingleton(Options.Create(new LegacySettings { ApiKey = ValidApiKey }));

        _app = builder.Build();

        // Route through the real /legacy group (LegacyApiKeyFilter + MapLegacyEndpoints), not
        // MapNewTournament() directly, so this test actually exercises the filter that protects the
        // route as deployed, and the relative path registered in NewTournament.cs.
        _app.MapLegacyGroup();

        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    [Fact(DisplayName = "POST /legacy/tournaments/new returns 401 and does not enqueue a job when the X-Api-Key header is missing")]
    public async Task Post_ShouldReturn401AndNotEnqueue_WhenApiKeyHeaderIsMissing()
    {
        // Arrange
        using var client = _app.GetTestClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/tournaments/new",
            new NewTournamentRequest(42),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /legacy/tournaments/new returns 401 and does not enqueue a job when the X-Api-Key header is wrong")]
    public async Task Post_ShouldReturn401AndNotEnqueue_WhenApiKeyHeaderIsWrong()
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/tournaments/new",
            new NewTournamentRequest(42),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /legacy/tournaments/new returns 400 and does not enqueue a job when TournamentId is invalid")]
    public async Task Post_ShouldReturn400AndNotEnqueue_WhenTournamentIdIsInvalid()
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ValidApiKey);

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/tournaments/new",
            new NewTournamentRequest(0),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /legacy/tournaments/new returns 202 and enqueues a NewTournamentSyncJob with the request's TournamentId")]
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
            "/legacy/tournaments/new",
            new NewTournamentRequest(42),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        capturedJob.ShouldNotBeNull();
        capturedJob.Type.ShouldBe(typeof(NewTournamentSyncJob));
        capturedJob.Method.Name.ShouldBe(nameof(NewTournamentSyncJob.SyncAsync));
        capturedJob.Args[0].ShouldBe(42);
    }
}

[UnitTest]
[Component("Legacy")]
public sealed class LegacyTournamentLinkExtensionsTests
{
    [Fact(DisplayName = "ApplyLegacyId should set the tournament's LegacyId")]
    public void ApplyLegacyId_ShouldSetLegacyId()
    {
        // Arrange
        var tournament = TournamentFactory.Create();

        // Act
        tournament.ApplyLegacyId(123);

        // Assert
        tournament.LegacyId.ShouldBe(123);
    }
}

[UnitTest]
[Component("Legacy")]
public sealed class TournamentLinkCannotBeDerivedEmailTests
{
    [Fact(DisplayName = "ToHtmlBody should include the legacy id, end date, and candidate count")]
    public void ToHtmlBody_ShouldIncludeLegacyIdEndDateAndCandidateCount()
    {
        // Arrange
        var email = new TournamentLinkCannotBeDerivedEmail(42, new DateOnly(2025, 10, 5), 3);

        // Act
        var body = email.ToHtmlBody();

        // Assert
        body.ShouldContain("42");
        body.ShouldContain("2025-10-05");
        body.ShouldContain("3");
        body.ShouldContain("LegacyId");
    }

    [Fact(DisplayName = "ToHtmlBody should render zero candidates")]
    public void ToHtmlBody_ShouldRenderZeroCandidates_WhenNoCandidatesRemain()
    {
        // Arrange
        var email = new TournamentLinkCannotBeDerivedEmail(1, new DateOnly(2025, 1, 1), 0);

        // Act
        var body = email.ToHtmlBody();

        // Assert
        body.ShouldContain("remaining after narrowing: 0");
    }
}

[IntegrationTest]
[Component("Legacy")]
[Collection<AppDbContextFixture>]
public sealed class NewTournamentSyncJobTests(AppDbContextFixture fixture)
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
        // container for one temporary backdoor query. CREATE TEMP TABLEs are scoped to this single
        // connection - they're gone as soon as the connection closes, so they need no cleanup and
        // can't collide with the shared fixture's own schema/Respawn reset.
        _legacyConnection = new NpgsqlConnection(fixture.ConnectionString);
        await _legacyConnection.OpenAsync();

        // Unquoted identifiers here so Postgres folds them to lowercase, matching how it resolves
        // the production query's own unquoted column/table references.
        await using var createTournaments = _legacyConnection.CreateCommand();
        createTournaments.CommandText = """
            CREATE TEMP TABLE Tournaments (
                Id integer PRIMARY KEY,
                "end" timestamp NOT NULL
            )
            """;
        await createTournaments.ExecuteNonQueryAsync();

        await using var createSingles = _legacyConnection.CreateCommand();
        createSingles.CommandText = """
            CREATE TEMP TABLE Tournaments_SinglesTournament (
                Id integer PRIMARY KEY,
                TournamentType integer NOT NULL
            )
            """;
        await createSingles.ExecuteNonQueryAsync();

        await using var createTeam = _legacyConnection.CreateCommand();
        createTeam.CommandText = """
            CREATE TEMP TABLE Tournaments_TeamTournament (
                Id integer PRIMARY KEY,
                TeamSize integer NOT NULL,
                OverUnder boolean NULL
            )
            """;
        await createTeam.ExecuteNonQueryAsync();

        await using var createSquads = _legacyConnection.CreateCommand();
        createSquads.CommandText = """
            CREATE TEMP TABLE Squads (
                Id integer PRIMARY KEY,
                BowlingDate timestamp NOT NULL
            )
            """;
        await createSquads.ExecuteNonQueryAsync();

        await using var createSinglesSquad = _legacyConnection.CreateCommand();
        createSinglesSquad.CommandText = """
            CREATE TEMP TABLE Squads_SinglesSquad (
                Id integer PRIMARY KEY,
                TournamentId integer NOT NULL
            )
            """;
        await createSinglesSquad.ExecuteNonQueryAsync();

        await using var createTeamSquad = _legacyConnection.CreateCommand();
        createTeamSquad.CommandText = """
            CREATE TEMP TABLE Squads_TeamSquad (
                Id integer PRIMARY KEY,
                TournamentId integer NOT NULL
            )
            """;
        await createTeamSquad.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _legacyConnection.DisposeAsync();
        await _serviceProvider.DisposeAsync();
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task InsertLegacySinglesTournamentAsync(int id, DateTime end, int singlesTournamentType)
    {
        await using var insertTournament = _legacyConnection.CreateCommand();
        insertTournament.CommandText = """INSERT INTO Tournaments (Id, "end") VALUES (@Id, @End)""";
        insertTournament.Parameters.AddWithValue("@Id", id);
        insertTournament.Parameters.AddWithValue("@End", end);
        await insertTournament.ExecuteNonQueryAsync();

        await using var insertSingles = _legacyConnection.CreateCommand();
        insertSingles.CommandText = "INSERT INTO Tournaments_SinglesTournament (Id, TournamentType) VALUES (@Id, @Type)";
        insertSingles.Parameters.AddWithValue("@Id", id);
        insertSingles.Parameters.AddWithValue("@Type", singlesTournamentType);
        await insertSingles.ExecuteNonQueryAsync();
    }

    private async Task InsertLegacyTeamTournamentAsync(int id, DateTime end, int teamSize, bool? overUnder)
    {
        await using var insertTournament = _legacyConnection.CreateCommand();
        insertTournament.CommandText = """INSERT INTO Tournaments (Id, "end") VALUES (@Id, @End)""";
        insertTournament.Parameters.AddWithValue("@Id", id);
        insertTournament.Parameters.AddWithValue("@End", end);
        await insertTournament.ExecuteNonQueryAsync();

        await using var insertTeam = _legacyConnection.CreateCommand();
        insertTeam.CommandText = "INSERT INTO Tournaments_TeamTournament (Id, TeamSize, OverUnder) VALUES (@Id, @TeamSize, @OverUnder)";
        insertTeam.Parameters.AddWithValue("@Id", id);
        insertTeam.Parameters.AddWithValue("@TeamSize", teamSize);
        insertTeam.Parameters.AddWithValue("@OverUnder", overUnder.HasValue ? overUnder.Value : DBNull.Value);
        await insertTeam.ExecuteNonQueryAsync();
    }

    private async Task InsertLegacySinglesSquadAsync(int squadId, int legacyTournamentId, DateTime bowlingDate)
    {
        await using var insertSquad = _legacyConnection.CreateCommand();
        insertSquad.CommandText = "INSERT INTO Squads (Id, BowlingDate) VALUES (@Id, @BowlingDate)";
        insertSquad.Parameters.AddWithValue("@Id", squadId);
        insertSquad.Parameters.AddWithValue("@BowlingDate", bowlingDate);
        await insertSquad.ExecuteNonQueryAsync();

        await using var insertSinglesSquad = _legacyConnection.CreateCommand();
        insertSinglesSquad.CommandText = "INSERT INTO Squads_SinglesSquad (Id, TournamentId) VALUES (@Id, @TournamentId)";
        insertSinglesSquad.Parameters.AddWithValue("@Id", squadId);
        insertSinglesSquad.Parameters.AddWithValue("@TournamentId", legacyTournamentId);
        await insertSinglesSquad.ExecuteNonQueryAsync();
    }

    private async Task<SeasonId> CreateSeasonAsync(CancellationToken ct)
    {
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);
        return season.Id;
    }

    private async Task<Tournament> CreateUnlinkedTournamentAsync(SeasonId seasonId, TournamentType tournamentType, DateOnly endDate, CancellationToken ct)
    {
        var tournament = TournamentFactory.Create(tournamentType: tournamentType, startDate: endDate, endDate: endDate, seasonId: seasonId);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        return tournament;
    }

    private NewTournamentSyncJob CreateJob(Mock<IEmailSender>? emailSender = null, FakeLogger<NewTournamentSyncJob>? logger = null) =>
        new(
            _dbContext,
            _legacyConnection,
            _serviceProvider.GetRequiredService<IFusionCache>(),
            (emailSender ?? new Mock<IEmailSender>(MockBehavior.Strict)).Object,
            logger ?? new FakeLogger<NewTournamentSyncJob>());

    [Fact(DisplayName = "SyncAsync should be a no-op and should log information when the legacy id was already synced")]
    public async Task SyncAsync_ShouldBeNoOpAndLogInformation_WhenLegacyIdWasAlreadySynced()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var seasonId = await CreateSeasonAsync(ct);
        var tournament = TournamentFactory.Create(legacyId: 1, seasonId: seasonId);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        var fakeLogger = new FakeLogger<NewTournamentSyncJob>();
        var job = CreateJob(logger: fakeLogger);

        // Act
        await job.SyncAsync(1, ct);

        // Assert - Strict email mock: any SendAsync call without a setup would throw.
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldContain("1");
    }

    [Fact(DisplayName = "SyncAsync should evict the tournament's own cache tag when the legacy id was already synced")]
    public async Task SyncAsync_ShouldEvictTournamentCacheTag_WhenLegacyIdWasAlreadySynced()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var seasonId = await CreateSeasonAsync(ct);
        var tournament = TournamentFactory.Create(legacyId: 1, seasonId: seasonId);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string cacheKey = "already-linked-tournament-cache-test";
        await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("stale-cached-value"), tags: [$"neba:tournaments:{tournament.Id}"], token: ct);

        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert - a stale cached value would be returned by GetOrSetAsync instead of invoking the factory.
        var valueAfterSync = await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("fresh-value"), token: ct);
        valueAfterSync.ShouldBe("fresh-value");
    }

    [Fact(DisplayName = "SyncAsync should log a warning and make no changes when the legacy tournament is not found")]
    public async Task SyncAsync_ShouldLogWarningAndMakeNoChanges_WhenLegacyTournamentIsNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fakeLogger = new FakeLogger<NewTournamentSyncJob>();
        var job = CreateJob(logger: fakeLogger);

        // Act
        await job.SyncAsync(999, ct);

        // Assert - Strict email mock: any SendAsync call without a setup would throw.
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("999");
    }

    [Fact(DisplayName = "SyncAsync should link the only candidate tournament sharing the legacy end date")]
    public async Task SyncAsync_ShouldLinkOnlyCandidate_WhenExactlyOneTournamentSharesTheEndDate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var endDate = new DateOnly(2025, 10, 5);
        var seasonId = await CreateSeasonAsync(ct);
        var tournament = await CreateUnlinkedTournamentAsync(seasonId, TournamentType.Singles, endDate, ct);
        await InsertLegacySinglesTournamentAsync(1, endDate.ToDateTime(TimeOnly.MinValue), singlesTournamentType: 0);
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var linked = await _dbContext.Tournaments.SingleAsync(t => t.Id == tournament.Id, ct);
        linked.LegacyId.ShouldBe(1);
    }

    [Fact(DisplayName = "SyncAsync should evict the season's tournament list and the tournament's own cache tag when linking for the first time")]
    public async Task SyncAsync_ShouldEvictSeasonListAndTournamentCacheTags_WhenLinkingForTheFirstTime()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var endDate = new DateOnly(2025, 10, 5);
        var seasonId = await CreateSeasonAsync(ct);
        var tournament = await CreateUnlinkedTournamentAsync(seasonId, TournamentType.Singles, endDate, ct);
        await InsertLegacySinglesTournamentAsync(1, endDate.ToDateTime(TimeOnly.MinValue), singlesTournamentType: 0);

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string seasonListCacheKey = "tournament-season-list-cache-test";
        const string tournamentCacheKey = "tournament-detail-cache-test";
        await cache.GetOrSetAsync(seasonListCacheKey, _ => Task.FromResult("stale-cached-value"), tags: [$"neba:tournaments:{seasonId}"], token: ct);
        await cache.GetOrSetAsync(tournamentCacheKey, _ => Task.FromResult("stale-cached-value"), tags: [$"neba:tournaments:{tournament.Id}"], token: ct);

        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert - a stale cached value would be returned by GetOrSetAsync instead of invoking the factory.
        var seasonListValueAfterSync = await cache.GetOrSetAsync(seasonListCacheKey, _ => Task.FromResult("fresh-value"), token: ct);
        seasonListValueAfterSync.ShouldBe("fresh-value");
        var tournamentValueAfterSync = await cache.GetOrSetAsync(tournamentCacheKey, _ => Task.FromResult("fresh-value"), token: ct);
        tournamentValueAfterSync.ShouldBe("fresh-value");
    }

    [Fact(DisplayName = "SyncAsync should sync the tournament's squads when linking for the first time")]
    public async Task SyncAsync_ShouldSyncSquads_WhenLinkingTournamentForTheFirstTime()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var endDate = new DateOnly(2025, 10, 5);
        var seasonId = await CreateSeasonAsync(ct);
        var tournament = await CreateUnlinkedTournamentAsync(seasonId, TournamentType.Singles, endDate, ct);
        await InsertLegacySinglesTournamentAsync(1, endDate.ToDateTime(TimeOnly.MinValue), singlesTournamentType: 0);
        // 1:00 PM Eastern on Oct 5 2025 (EDT, -04:00) == 5:00 PM UTC.
        await InsertLegacySinglesSquadAsync(101, legacyTournamentId: 1, bowlingDate: new DateTime(2025, 10, 5, 13, 0, 0, DateTimeKind.Unspecified));
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var linked = await _dbContext.Tournaments.Include(t => t.Squads).SingleAsync(t => t.Id == tournament.Id, ct);
        var squad = linked.Squads.ShouldHaveSingleItem();
        squad.LegacyId.ShouldBe(101);
        squad.BowlingDateTimeUtc.ShouldBe(new DateTimeOffset(2025, 10, 5, 17, 0, 0, TimeSpan.Zero));
    }

    [Fact(DisplayName = "SyncAsync should sync new squads without duplicating already-synced ones when the legacy id was already synced")]
    public async Task SyncAsync_ShouldSyncNewSquadsWithoutDuplicating_WhenLegacyIdWasAlreadySynced()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var seasonId = await CreateSeasonAsync(ct);
        var existingSquad = SquadFactory.Create(
            bowlingDateTime: new DateTimeOffset(2025, 10, 5, 17, 0, 0, TimeSpan.Zero),
            legacyId: 101);
        var tournament = TournamentFactory.Create(
            startDate: new DateOnly(2025, 10, 5),
            endDate: new DateOnly(2025, 10, 5),
            legacyId: 1,
            seasonId: seasonId,
            squads: [existingSquad]);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        // Squad 101 is already synced (should be skipped); squad 102 is new (should be added).
        // 1:00 PM and 3:00 PM Eastern on Oct 5 2025 (EDT, -04:00) == 5:00 PM and 7:00 PM UTC.
        await InsertLegacySinglesSquadAsync(101, legacyTournamentId: 1, bowlingDate: new DateTime(2025, 10, 5, 13, 0, 0, DateTimeKind.Unspecified));
        await InsertLegacySinglesSquadAsync(102, legacyTournamentId: 1, bowlingDate: new DateTime(2025, 10, 5, 15, 0, 0, DateTimeKind.Unspecified));
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var linked = await _dbContext.Tournaments.Include(t => t.Squads).SingleAsync(t => t.Id == tournament.Id, ct);
        linked.Squads.Count.ShouldBe(2);
        linked.Squads.ShouldContain(s => s.LegacyId == 101);
        linked.Squads.ShouldContain(s => s.LegacyId == 102 && s.BowlingDateTimeUtc == new DateTimeOffset(2025, 10, 5, 19, 0, 0, TimeSpan.Zero));
    }

    [Fact(DisplayName = "SyncAsync should log a warning and leave the tournament's squads unchanged when a legacy squad's date is outside the tournament's range")]
    public async Task SyncAsync_ShouldLogWarningAndMakeNoSquadChanges_WhenLegacySquadDateIsOutOfRange()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var seasonId = await CreateSeasonAsync(ct);
        var tournament = TournamentFactory.Create(
            startDate: new DateOnly(2025, 10, 5),
            endDate: new DateOnly(2025, 10, 5),
            legacyId: 1,
            seasonId: seasonId);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        // Two days after the tournament's only date - well outside the valid range regardless of offset.
        await InsertLegacySinglesSquadAsync(201, legacyTournamentId: 1, bowlingDate: new DateTime(2025, 10, 7, 13, 0, 0, DateTimeKind.Unspecified));
        var fakeLogger = new FakeLogger<NewTournamentSyncJob>();
        var job = CreateJob(logger: fakeLogger);

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var linked = await _dbContext.Tournaments.Include(t => t.Squads).SingleAsync(t => t.Id == tournament.Id, ct);
        linked.Squads.ShouldBeEmpty();

        // The already-linked branch also logs an info entry; the squad sync failure logs its own warning alongside it.
        var record = fakeLogger.Collector.GetSnapshot().Single(r => r.Level == LogLevel.Warning);
        record.Message.ShouldContain("201");
        record.Message.ShouldContain("1");
    }

    [Fact(DisplayName = "SyncAsync should narrow candidates to the team tournament when the legacy row is a team tournament")]
    public async Task SyncAsync_ShouldNarrowToTeamCandidate_WhenLegacyRowIsTeamTournament()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var endDate = new DateOnly(2025, 10, 5);
        var seasonId = await CreateSeasonAsync(ct);
        await CreateUnlinkedTournamentAsync(seasonId, TournamentType.Singles, endDate, ct);
        var doubles = await CreateUnlinkedTournamentAsync(seasonId, TournamentType.Doubles, endDate, ct);
        await InsertLegacyTeamTournamentAsync(1, endDate.ToDateTime(TimeOnly.MinValue), teamSize: 2, overUnder: false);
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var linked = await _dbContext.Tournaments.SingleAsync(t => t.Id == doubles.Id, ct);
        linked.LegacyId.ShouldBe(1);
        var singles = await _dbContext.Tournaments.SingleAsync(t => t.TournamentType == TournamentType.Singles, ct);
        singles.LegacyId.ShouldBeNull();
    }

    [Theory(DisplayName = "SyncAsync should link the team candidate matching the legacy team shape")]
    [InlineData(2, false, "Doubles")]
    [InlineData(2, true, "Over/Under 50 Doubles")]
    [InlineData(3, false, "Trios")]
    [InlineData(5, false, "Baker")]
    public async Task SyncAsync_ShouldLinkTeamCandidateMatchingLegacyTeamShape_WhenMultipleTeamCandidatesShareTheEndDate(
        int teamSize, bool? overUnder, string expectedTournamentTypeName)
    {
        // Arrange - no IsBaker column exists on the legacy side; Baker must be derived from team size 5,
        // the same way Doubles (2) and Trios (3) are already derived.
        var ct = TestContext.Current.CancellationToken;
        var endDate = new DateOnly(2025, 10, 5);
        var seasonId = await CreateSeasonAsync(ct);
        var doubles = await CreateUnlinkedTournamentAsync(seasonId, TournamentType.Doubles, endDate, ct);
        var trios = await CreateUnlinkedTournamentAsync(seasonId, TournamentType.Trios, endDate, ct);
        var baker = await CreateUnlinkedTournamentAsync(seasonId, TournamentType.Baker, endDate, ct);
        var overUnderDoubles = await CreateUnlinkedTournamentAsync(seasonId, TournamentType.OverUnderFiftyDoubles, endDate, ct);
        await InsertLegacyTeamTournamentAsync(1, endDate.ToDateTime(TimeOnly.MinValue), teamSize, overUnder);
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var expectedTournamentType = TournamentType.FromName(expectedTournamentTypeName);
        var linked = await _dbContext.Tournaments.SingleAsync(t => t.LegacyId == 1, ct);
        linked.TournamentType.ShouldBe(expectedTournamentType);

        foreach (var unlinked in new[] { doubles, trios, baker, overUnderDoubles }.Where(t => t.TournamentType != expectedTournamentType))
        {
            var candidate = await _dbContext.Tournaments.SingleAsync(t => t.Id == unlinked.Id, ct);
            candidate.LegacyId.ShouldBeNull();
        }
    }

    [Theory(DisplayName = "SyncAsync should link the singles candidate matching the legacy singles type")]
    [InlineData(0, "Singles")]
    [InlineData(1, "Non-Champions")]
    [InlineData(2, "Senior")]
    [InlineData(3, "Women")]
    [InlineData(4, "Tournament of Champions")]
    [InlineData(5, "Invitational")]
    [InlineData(6, "Masters")]
    [InlineData(7, "Youth")]
    [InlineData(8, "Senior / Women")]
    public async Task SyncAsync_ShouldLinkSinglesCandidateMatchingLegacySinglesType_WhenMultipleSinglesCandidatesShareTheEndDate(
        int legacySinglesType, string expectedTournamentTypeName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var endDate = new DateOnly(2025, 10, 5);
        var seasonId = await CreateSeasonAsync(ct);
        TournamentType[] singlesTypes =
        [
            TournamentType.Singles, TournamentType.NonChampions, TournamentType.Senior, TournamentType.Women,
            TournamentType.TournamentOfChampions, TournamentType.Invitational, TournamentType.Masters,
            TournamentType.Youth, TournamentType.SeniorAndWomen
        ];
        foreach (var type in singlesTypes)
        {
            await CreateUnlinkedTournamentAsync(seasonId, type, endDate, ct);
        }
        await InsertLegacySinglesTournamentAsync(1, endDate.ToDateTime(TimeOnly.MinValue), legacySinglesType);
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var expectedTournamentType = TournamentType.FromName(expectedTournamentTypeName);
        var linked = await _dbContext.Tournaments.SingleAsync(t => t.LegacyId == 1, ct);
        linked.TournamentType.ShouldBe(expectedTournamentType);
    }

    [Fact(DisplayName = "SyncAsync should send a manual-intervention email and leave tournaments unlinked when no tournament shares the legacy end date")]
    public async Task SyncAsync_ShouldSendEmailAndLeaveUnlinked_WhenNoTournamentSharesTheEndDate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var endDate = new DateOnly(2025, 10, 5);
        await InsertLegacySinglesTournamentAsync(1, endDate.ToDateTime(TimeOnly.MinValue), singlesTournamentType: 0);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        EmailMessage? sentMessage = null;
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => sentMessage = message)
            .Returns(Task.CompletedTask);

        var fakeLogger = new FakeLogger<NewTournamentSyncJob>();
        var job = CreateJob(emailSender, fakeLogger);

        // Act
        await job.SyncAsync(1, ct);

        // Assert - Strict mock: the Setup above is the verification that SendAsync was called.
        sentMessage.ShouldNotBeNull();
        sentMessage.To.ShouldBe("website@bowlneba.com");
        sentMessage.Subject.ShouldBe("Manual intervention needed: tournament link");
        sentMessage.HtmlBody.ShouldContain("1");
        sentMessage.HtmlBody.ShouldContain("remaining after narrowing: 0");

        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
    }

    [Fact(DisplayName = "SyncAsync should send a manual-intervention email and leave tournaments unlinked when the legacy team size has no website equivalent")]
    public async Task SyncAsync_ShouldSendEmailAndLeaveUnlinked_WhenLegacyTeamSizeHasNoWebsiteEquivalent()
    {
        // Arrange - team size 4 is not a recognized website team format; per MapLegacyTournamentType's
        // contract, an unmapped shape skips the exact-type narrowing step rather than eliminating every
        // remaining candidate, so both team candidates below stay ambiguous.
        var ct = TestContext.Current.CancellationToken;
        var endDate = new DateOnly(2025, 10, 5);
        var seasonId = await CreateSeasonAsync(ct);
        var doubles = await CreateUnlinkedTournamentAsync(seasonId, TournamentType.Doubles, endDate, ct);
        var trios = await CreateUnlinkedTournamentAsync(seasonId, TournamentType.Trios, endDate, ct);
        await InsertLegacyTeamTournamentAsync(1, endDate.ToDateTime(TimeOnly.MinValue), teamSize: 4, overUnder: null);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var job = CreateJob(emailSender);

        // Act
        await job.SyncAsync(1, ct);

        // Assert - Strict mock: the Setup above is the verification that SendAsync was called.
        var unchangedDoubles = await _dbContext.Tournaments.SingleAsync(t => t.Id == doubles.Id, ct);
        var unchangedTrios = await _dbContext.Tournaments.SingleAsync(t => t.Id == trios.Id, ct);
        unchangedDoubles.LegacyId.ShouldBeNull();
        unchangedTrios.LegacyId.ShouldBeNull();
    }

    [Fact(DisplayName = "SyncAsync should send a manual-intervention email and leave tournaments unlinked when multiple candidates share the same exact type")]
    public async Task SyncAsync_ShouldSendEmailAndLeaveUnlinked_WhenMultipleCandidatesShareTheSameExactType()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var endDate = new DateOnly(2025, 10, 5);
        var seasonId = await CreateSeasonAsync(ct);
        var first = await CreateUnlinkedTournamentAsync(seasonId, TournamentType.Singles, endDate, ct);
        var second = await CreateUnlinkedTournamentAsync(seasonId, TournamentType.Singles, endDate, ct);
        await InsertLegacySinglesTournamentAsync(1, endDate.ToDateTime(TimeOnly.MinValue), singlesTournamentType: 0);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        EmailMessage? sentMessage = null;
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => sentMessage = message)
            .Returns(Task.CompletedTask);
        var job = CreateJob(emailSender);

        // Act
        await job.SyncAsync(1, ct);

        // Assert - Strict mock: the Setup above is the verification that SendAsync was called.
        sentMessage.ShouldNotBeNull();
        sentMessage.HtmlBody.ShouldContain("remaining after narrowing: 2");
        var unchangedFirst = await _dbContext.Tournaments.SingleAsync(t => t.Id == first.Id, ct);
        var unchangedSecond = await _dbContext.Tournaments.SingleAsync(t => t.Id == second.Id, ct);
        unchangedFirst.LegacyId.ShouldBeNull();
        unchangedSecond.LegacyId.ShouldBeNull();
    }
}