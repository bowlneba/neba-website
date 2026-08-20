using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Database;
using Neba.Api.Email;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Legacy.Tournaments.Stats;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

using Npgsql;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Legacy.Tournaments.Stats;

// Exercises GenerateSeasonStatsJob end to end against real Dapper queries (a Postgres temp schema
// standing in for neba-fwk's Tournaments/Stats/Bowlers/Memberships/Credits/Cups tables) and a real
// AppDbContext - proves the raw SQL joins return the rows LegacySeasonStatsCalculator (covered in
// isolation elsewhere) expects, and covers the delete-then-regenerate idempotency contract this
// backdoor action requires. Mirrors SyncTournamentResultsJobTests's shape.
[IntegrationTest]
[Component("Legacy")]
[Collection<AppDbContextFixture>]
public sealed class GenerateSeasonStatsJobTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private const int NewMemberMembershipTypeId = 1;

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private NpgsqlConnection _legacyConnection = null!;
    private ServiceProvider _serviceProvider = null!;
    private int _nextStatsId = 1;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();

        var services = new ServiceCollection();
        services.AddFusionCache().WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromHours(1));
        _serviceProvider = services.BuildServiceProvider();

        // Same rationale as SyncTournamentResultsJobTests: plain Dapper works against any real
        // IDbConnection, so Postgres stands in for neba-fwk's MSSQL here.
        _legacyConnection = new NpgsqlConnection(fixture.ConnectionString);
        await _legacyConnection.OpenAsync();

        await using var create = _legacyConnection.CreateCommand();
        create.CommandText = """
            CREATE TEMP TABLE Tournaments (
                Id integer PRIMARY KEY,
                Start timestamp NOT NULL,
                "end" timestamp NOT NULL,
                YearlyStatEligible boolean NOT NULL
            );
            CREATE TEMP TABLE Tournaments_SinglesTournament (
                Id integer PRIMARY KEY,
                TournamentType integer NOT NULL
            );
            CREATE TEMP TABLE Stats (
                Id integer PRIMARY KEY,
                BowlerId integer NOT NULL,
                TournamentId integer NOT NULL
            );
            CREATE TEMP TABLE Stats_QualifyingStats (
                Id integer PRIMARY KEY,
                SquadId integer NOT NULL,
                Score integer NOT NULL,
                Games integer NOT NULL,
                HighGame integer NOT NULL
            );
            CREATE TEMP TABLE Stats_MatchPlayStats (
                Id integer PRIMARY KEY,
                Score integer NOT NULL,
                Games integer NOT NULL,
                HighGame integer NOT NULL,
                Winner boolean NOT NULL
            );
            CREATE TEMP TABLE Stats_ResultsStats (
                Id integer PRIMARY KEY,
                SideCut integer NULL
            );
            CREATE TEMP TABLE Bowlers (
                Id integer PRIMARY KEY,
                Gender integer NULL,
                DateOfBirth date NULL
            );
            CREATE TEMP TABLE Memberships (
                Id integer PRIMARY KEY,
                Name varchar(30) NOT NULL
            );
            CREATE TEMP TABLE BowlerMemberships (
                Id integer PRIMARY KEY,
                BowlerId integer NOT NULL,
                MembershipId integer NOT NULL,
                EndDate date NOT NULL
            );
            CREATE TEMP TABLE Credits (
                Id integer PRIMARY KEY,
                Amount numeric NOT NULL,
                IssuedDate timestamp NOT NULL
            );
            CREATE TEMP TABLE Credits_BowlerCredit (
                Id integer PRIMARY KEY,
                BowlerId integer NOT NULL,
                Taxable smallint NOT NULL
            );
            CREATE TEMP TABLE Cups (
                Id integer PRIMARY KEY,
                "end" timestamp NOT NULL
            );
            CREATE TEMP TABLE CupResults (
                Id integer PRIMARY KEY,
                CupId integer NOT NULL,
                BowlerId integer NOT NULL,
                Payout numeric NOT NULL
            );
            """;
        await create.ExecuteNonQueryAsync();

        // Every SyncAsync call unconditionally looks up the "New Member" membership type -
        // every test needs at least this one row present.
        await using var insertMembershipType = _legacyConnection.CreateCommand();
        insertMembershipType.CommandText = "INSERT INTO Memberships (Id, Name) VALUES (@Id, @Name)";
        insertMembershipType.Parameters.AddWithValue("@Id", NewMemberMembershipTypeId);
        insertMembershipType.Parameters.AddWithValue("@Name", "New Member");
        await insertMembershipType.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _legacyConnection.DisposeAsync();
        await _serviceProvider.DisposeAsync();
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private GenerateSeasonStatsJob CreateJob(Mock<IEmailSender>? emailSender = null, FakeLogger<GenerateSeasonStatsJob>? logger = null) =>
        new(
            _dbContext,
            _legacyConnection,
            _serviceProvider.GetRequiredService<IFusionCache>(),
            (emailSender ?? new Mock<IEmailSender>(MockBehavior.Strict)).Object,
            logger ?? new FakeLogger<GenerateSeasonStatsJob>());

    private async Task<(Season Season, Tournament Tournament)> CreateSeasonAndTournamentAsync(
        int legacyTournamentId, DateOnly seasonStart, DateOnly seasonEnd, CancellationToken ct)
    {
        var season = SeasonFactory.Create(startDate: seasonStart, endDate: seasonEnd);
        await _dbContext.Seasons.AddAsync(season, ct);

        var tournament = TournamentFactory.Create(legacyId: legacyTournamentId, seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Deliberately not clearing the ChangeTracker here: several tests call tournament.AddResult(...)
        // directly on this returned, still-tracked instance later. Clearing would detach it, and
        // AddResult's mutation would never reach SaveChangesAsync.
        return (season, tournament);
    }

    private async Task<Bowler> CreateBowlerAsync(int legacyBowlerId, CancellationToken ct)
    {
        var bowler = BowlerFactory.Create(legacyId: legacyBowlerId);
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await _dbContext.SaveChangesAsync(ct);

        return bowler;
    }

    private async Task InsertLegacyTournamentAsync(int legacyTournamentId, DateTime start, DateTime end, bool yearlyStatEligible)
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = """INSERT INTO Tournaments (Id, Start, "end", YearlyStatEligible) VALUES (@Id, @Start, @End, @Eligible)""";
        insert.Parameters.AddWithValue("@Id", legacyTournamentId);
        insert.Parameters.AddWithValue("@Start", start);
        insert.Parameters.AddWithValue("@End", end);
        insert.Parameters.AddWithValue("@Eligible", yearlyStatEligible);
        await insert.ExecuteNonQueryAsync();
    }

    private async Task<int> InsertStatsAsync(int legacyBowlerId, int legacyTournamentId)
    {
        var statsId = _nextStatsId++;
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = "INSERT INTO Stats (Id, BowlerId, TournamentId) VALUES (@Id, @BowlerId, @TournamentId)";
        insert.Parameters.AddWithValue("@Id", statsId);
        insert.Parameters.AddWithValue("@BowlerId", legacyBowlerId);
        insert.Parameters.AddWithValue("@TournamentId", legacyTournamentId);
        await insert.ExecuteNonQueryAsync();
        return statsId;
    }

    private async Task InsertQualifyingStatsAsync(int statsId, int squadId, int score, int games, int highGame)
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = "INSERT INTO Stats_QualifyingStats (Id, SquadId, Score, Games, HighGame) VALUES (@Id, @SquadId, @Score, @Games, @HighGame)";
        insert.Parameters.AddWithValue("@Id", statsId);
        insert.Parameters.AddWithValue("@SquadId", squadId);
        insert.Parameters.AddWithValue("@Score", score);
        insert.Parameters.AddWithValue("@Games", games);
        insert.Parameters.AddWithValue("@HighGame", highGame);
        await insert.ExecuteNonQueryAsync();
    }

    private async Task InsertResultsStatsAsync(int statsId, int? sideCut)
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = "INSERT INTO Stats_ResultsStats (Id, SideCut) VALUES (@Id, @SideCut)";
        insert.Parameters.AddWithValue("@Id", statsId);
        insert.Parameters.AddWithValue("@SideCut", sideCut.HasValue ? sideCut.Value : DBNull.Value);
        await insert.ExecuteNonQueryAsync();
    }

    private async Task<Tournament> ReloadTournamentAsync(int legacyTournamentId, CancellationToken ct) =>
        await _dbContext.Set<Tournament>().SingleAsync(t => t.LegacyId == legacyTournamentId, ct);

    [Fact(DisplayName = "SyncAsync should log a warning and send an email when the legacy tournament has no linked website tournament")]
    public async Task SyncAsync_ShouldLogWarningAndSendEmail_WhenTournamentNotSynced()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fakeLogger = new FakeLogger<GenerateSeasonStatsJob>();
        EmailMessage? sentMessage = null;
        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => sentMessage = message)
            .Returns(Task.CompletedTask);
        var job = CreateJob(emailSender, fakeLogger);

        // Act
        await job.SyncAsync(999, ct);

        // Assert
        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Warning && r.Message.Contains("999", StringComparison.Ordinal));
        sentMessage.ShouldNotBeNull();
        sentMessage.HtmlBody.ShouldContain("999");
    }

    [Fact(DisplayName = "SyncAsync should regenerate a BowlerSeasonStats row from legacy qualifying/results data for a linked, mapped bowler")]
    public async Task SyncAsync_ShouldRegenerateBowlerSeasonStats_FromLegacyData()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (season, tournament) = await CreateSeasonAndTournamentAsync(
            legacyTournamentId: 42, seasonStart: new DateOnly(2026, 1, 1), seasonEnd: new DateOnly(2026, 12, 31), ct);
        var bowler = await CreateBowlerAsync(legacyBowlerId: 100, ct);

        await InsertLegacyTournamentAsync(42, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc), yearlyStatEligible: true);
        var statsId = await InsertStatsAsync(100, 42);
        await InsertQualifyingStatsAsync(statsId, squadId: 1, score: 1200, games: 6, highGame: 220);
        await InsertResultsStatsAsync(statsId, sideCut: null);

        tournament.CompleteTournament();
        tournament.AddResult(bowler.Id, place: 1, prizeMoney: 500m, points: 100);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        var job = CreateJob();

        // Act
        await job.SyncAsync(42, ct);

        // Assert
        var row = await _dbContext.BowlerSeasonStats.SingleAsync(s => s.SeasonId == season.Id && s.BowlerId == bowler.Id, ct);
        row.EligibleTournaments.ShouldBe(1);
        row.TotalTournaments.ShouldBe(1);
        row.BowlerOfTheYearPoints.ShouldBe(100);
        row.TournamentWinnings.ShouldBe(500m);
        row.QualifyingHighGame.ShouldBe(220);
    }

    [Fact(DisplayName = "SyncAsync should delete stale rows and converge to the same result when run twice for the same tournament")]
    public async Task SyncAsync_ShouldDeleteStaleRowsAndConverge_WhenRunTwice()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (season, tournament) = await CreateSeasonAndTournamentAsync(
            legacyTournamentId: 42, seasonStart: new DateOnly(2026, 1, 1), seasonEnd: new DateOnly(2026, 12, 31), ct);
        var bowler = await CreateBowlerAsync(legacyBowlerId: 100, ct);

        await InsertLegacyTournamentAsync(42, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc), yearlyStatEligible: true);
        var statsId = await InsertStatsAsync(100, 42);
        await InsertQualifyingStatsAsync(statsId, squadId: 1, score: 1200, games: 6, highGame: 220);
        await InsertResultsStatsAsync(statsId, sideCut: null);

        tournament.CompleteTournament();
        tournament.AddResult(bowler.Id, place: 1, prizeMoney: 500m, points: 100);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        var job = CreateJob();

        // Act
        await job.SyncAsync(42, ct);
        _dbContext.ChangeTracker.Clear();
        await job.SyncAsync(42, ct);

        // Assert - exactly one row survives per (season, bowler), not a duplicate from the second run.
        var rows = await _dbContext.BowlerSeasonStats.Where(s => s.SeasonId == season.Id).ToListAsync(ct);
        var row = rows.ShouldHaveSingleItem();
        row.BowlerId.ShouldBe(bowler.Id);
        row.BowlerOfTheYearPoints.ShouldBe(100);
    }

    [Fact(DisplayName = "SyncAsync should remove a bowler's stale row when they no longer map to a website bowler on a later run")]
    public async Task SyncAsync_ShouldRemoveStaleBowlerRow_WhenBowlerNoLongerAppearsInLegacyData()
    {
        // Arrange - two bowlers qualify on the first run; before the second run, the second
        // bowler's website Bowler.LegacyId link is removed (a correction), simulating the
        // delete-then-regenerate "as if the rows never existed" behavior the plan calls for.
        // Deleting the legacy Stats rows alone would not reproduce this: results are sourced from
        // the website's own already-synced TournamentResult (see the Decision Recap), which is
        // unaffected by a change to raw legacy data - only the website-side mapping controls whether
        // a computed result actually gets persisted as a BowlerSeasonStats row.
        var ct = TestContext.Current.CancellationToken;
        var (season, tournament) = await CreateSeasonAndTournamentAsync(
            legacyTournamentId: 42, seasonStart: new DateOnly(2026, 1, 1), seasonEnd: new DateOnly(2026, 12, 31), ct);
        var keptBowler = await CreateBowlerAsync(legacyBowlerId: 100, ct);
        var removedBowler = await CreateBowlerAsync(legacyBowlerId: 200, ct);

        await InsertLegacyTournamentAsync(42, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc), yearlyStatEligible: true);
        var keptStatsId = await InsertStatsAsync(100, 42);
        await InsertQualifyingStatsAsync(keptStatsId, squadId: 1, score: 1200, games: 6, highGame: 220);
        await InsertResultsStatsAsync(keptStatsId, sideCut: null);
        var removedStatsId = await InsertStatsAsync(200, 42);
        await InsertQualifyingStatsAsync(removedStatsId, squadId: 1, score: 1100, games: 6, highGame: 210);
        await InsertResultsStatsAsync(removedStatsId, sideCut: null);

        tournament.CompleteTournament();
        tournament.AddResult(keptBowler.Id, place: 1, prizeMoney: 500m, points: 100);
        tournament.AddResult(removedBowler.Id, place: 2, prizeMoney: 250m, points: 50);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        var job = CreateJob();
        await job.SyncAsync(42, ct);
        _dbContext.ChangeTracker.Clear();

        // Act - simulate a correction: the second bowler is unlinked from the legacy system. This
        // makes them "unmapped" on the second run (still present in raw legacy qualifying/results
        // data), which the job reports via its unmapped-bowler email - expected here, not a failure.
        await _dbContext.Bowlers
            .Where(b => b.Id == removedBowler.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.LegacyId, (int?)null), ct);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var secondRunJob = CreateJob(emailSender);

        await secondRunJob.SyncAsync(42, ct);

        // Assert
        var rows = await _dbContext.BowlerSeasonStats.Where(s => s.SeasonId == season.Id).ToListAsync(ct);
        var row = rows.ShouldHaveSingleItem();
        row.BowlerId.ShouldBe(keptBowler.Id);
    }

    [Fact(DisplayName = "SyncAsync should evict the season stats cache tag after saving")]
    public async Task SyncAsync_ShouldEvictSeasonStatsCacheTag_AfterSaving()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (season, tournament) = await CreateSeasonAndTournamentAsync(
            legacyTournamentId: 42, seasonStart: new DateOnly(2026, 1, 1), seasonEnd: new DateOnly(2026, 12, 31), ct);
        var bowler = await CreateBowlerAsync(legacyBowlerId: 100, ct);

        await InsertLegacyTournamentAsync(42, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc), yearlyStatEligible: true);
        var statsId = await InsertStatsAsync(100, 42);
        await InsertQualifyingStatsAsync(statsId, squadId: 1, score: 1200, games: 6, highGame: 220);
        await InsertResultsStatsAsync(statsId, sideCut: null);

        tournament.CompleteTournament();
        tournament.AddResult(bowler.Id, place: 1, prizeMoney: 500m, points: 100);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var cacheKey = $"season-stats-cache-test:{season.Id}";
        await cache.GetOrSetAsync(
            cacheKey,
            _ => Task.FromResult("stale-cached-value"),
            tags: [$"neba:stats:seasons:{season.Id}"],
            token: ct);

        var job = CreateJob();

        // Act
        await job.SyncAsync(42, ct);

        // Assert - a stale cached value would be returned by GetOrSetAsync instead of invoking the factory.
        var valueAfterSync = await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("fresh-value"), token: ct);
        valueAfterSync.ShouldBe("fresh-value");
    }

    [Fact(DisplayName = "SyncAsync should log a warning and send an email for a legacy bowler with no matching website bowler")]
    public async Task SyncAsync_ShouldLogWarningAndSendEmail_ForUnmappedLegacyBowler()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await CreateSeasonAndTournamentAsync(
            legacyTournamentId: 42, seasonStart: new DateOnly(2026, 1, 1), seasonEnd: new DateOnly(2026, 12, 31), ct);
        // No CreateBowlerAsync call - legacy bowler 999 has no website match.

        await InsertLegacyTournamentAsync(42, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc), yearlyStatEligible: true);
        var statsId = await InsertStatsAsync(999, 42);
        await InsertQualifyingStatsAsync(statsId, squadId: 1, score: 1200, games: 6, highGame: 220);
        await InsertResultsStatsAsync(statsId, sideCut: null);

        var fakeLogger = new FakeLogger<GenerateSeasonStatsJob>();
        EmailMessage? sentMessage = null;
        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => sentMessage = message)
            .Returns(Task.CompletedTask);
        var job = CreateJob(emailSender, fakeLogger);

        // Act
        await job.SyncAsync(42, ct);

        // Assert
        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Warning && r.Message.Contains("999", StringComparison.Ordinal));
        sentMessage.ShouldNotBeNull();
        sentMessage.HtmlBody.ShouldContain("999");
    }

    [Fact(DisplayName = "SyncAsync should log an error and continue without throwing when a bowler has more than one Stats_ResultsStats row for the same tournament")]
    public async Task SyncAsync_ShouldLogErrorAndContinue_WhenBowlerHasMultipleSideCutRows()
    {
        // Arrange - data anomaly: two Stats rows for the same bowler+tournament, each with its own
        // Stats_ResultsStats.SideCut. Regression coverage for the duplicate-key crash this guards.
        var ct = TestContext.Current.CancellationToken;
        var (_, tournament) = await CreateSeasonAndTournamentAsync(
            legacyTournamentId: 42, seasonStart: new DateOnly(2026, 1, 1), seasonEnd: new DateOnly(2026, 12, 31), ct);
        var bowler = await CreateBowlerAsync(legacyBowlerId: 100, ct);

        await InsertLegacyTournamentAsync(42, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc), yearlyStatEligible: true);
        var firstStatsId = await InsertStatsAsync(100, 42);
        await InsertQualifyingStatsAsync(firstStatsId, squadId: 1, score: 1200, games: 6, highGame: 220);
        await InsertResultsStatsAsync(firstStatsId, sideCut: null);
        var secondStatsId = await InsertStatsAsync(100, 42);
        await InsertResultsStatsAsync(secondStatsId, sideCut: 1);

        tournament.CompleteTournament();
        tournament.AddResult(bowler.Id, place: 1, prizeMoney: 500m, points: 100);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        var fakeLogger = new FakeLogger<GenerateSeasonStatsJob>();
        var job = CreateJob(logger: fakeLogger);

        // Act & Assert
        await Should.NotThrowAsync(() => job.SyncAsync(42, ct));

        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Error && r.Message.Contains("100", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "SyncAsync should only read legacy stats for tournaments within the target season's date range")]
    public async Task SyncAsync_ShouldOnlyReadStatsWithinSeasonDateRange()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (season, tournament) = await CreateSeasonAndTournamentAsync(
            legacyTournamentId: 42, seasonStart: new DateOnly(2026, 1, 1), seasonEnd: new DateOnly(2026, 12, 31), ct);
        var inSeasonBowler = await CreateBowlerAsync(legacyBowlerId: 100, ct);
        await CreateBowlerAsync(legacyBowlerId: 200, ct);

        await InsertLegacyTournamentAsync(42, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc), yearlyStatEligible: true);
        var inSeasonStatsId = await InsertStatsAsync(100, 42);
        await InsertQualifyingStatsAsync(inSeasonStatsId, squadId: 1, score: 1200, games: 6, highGame: 220);
        await InsertResultsStatsAsync(inSeasonStatsId, sideCut: null);

        // A tournament from the prior season - must not contribute to this season's regenerate.
        await InsertLegacyTournamentAsync(43, new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 3, 2, 0, 0, 0, DateTimeKind.Utc), yearlyStatEligible: true);
        var outOfSeasonStatsId = await InsertStatsAsync(200, 43);
        await InsertQualifyingStatsAsync(outOfSeasonStatsId, squadId: 1, score: 1000, games: 6, highGame: 190);
        await InsertResultsStatsAsync(outOfSeasonStatsId, sideCut: null);

        tournament.CompleteTournament();
        tournament.AddResult(inSeasonBowler.Id, place: 1, prizeMoney: 500m, points: 100);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        var job = CreateJob();

        // Act
        await job.SyncAsync(42, ct);

        // Assert
        var rows = await _dbContext.BowlerSeasonStats.Where(s => s.SeasonId == season.Id).ToListAsync(ct);
        var row = rows.ShouldHaveSingleItem();
        row.BowlerId.ShouldBe(inSeasonBowler.Id);
        _ = await ReloadTournamentAsync(42, ct);
    }
}
