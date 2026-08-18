using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Database;
using Neba.Api.Email;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Legacy.Tournaments.Complete;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

using Npgsql;

namespace Neba.Api.Tests.Legacy.Tournaments.Complete;

// Exercises SyncTournamentResultsJob end to end against real Dapper queries (a Postgres temp
// schema standing in for neba-fwk's Stats/Stats_ResultsStats/Stats_QualifyingStats/Teams/
// TeamMember/SquadTeams tables) and a real AppDbContext - this is what proves the raw SQL joins
// return the rows TournamentPlaceCalculator (covered in isolation elsewhere) expects, and covers
// the idempotency contract this backdoor action requires.
[IntegrationTest]
[Component("Legacy")]
[Collection<AppDbContextFixture>]
public sealed class SyncTournamentResultsJobTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private NpgsqlConnection _legacyConnection = null!;
    private int _nextStatsId = 1;
    private int _nextSquadTeamsId = 1;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();

        // Same rationale as SyncSquadScoresSyncJobTests: plain Dapper works against any real
        // IDbConnection, so Postgres stands in for neba-fwk's MSSQL here. A CREATE TEMP TABLE is
        // scoped to this single connection and needs no cleanup.
        _legacyConnection = new NpgsqlConnection(fixture.ConnectionString);
        await _legacyConnection.OpenAsync();

        await using var createStats = _legacyConnection.CreateCommand();
        createStats.CommandText = """
            CREATE TEMP TABLE Stats (
                Id integer PRIMARY KEY,
                BowlerId integer NOT NULL,
                TournamentId integer NOT NULL
            );
            CREATE TEMP TABLE Stats_ResultsStats (
                Id integer PRIMARY KEY,
                Place integer NULL,
                Payout numeric NOT NULL,
                Points integer NOT NULL
            );
            CREATE TEMP TABLE Stats_QualifyingStats (
                Id integer PRIMARY KEY,
                SquadId integer NOT NULL,
                Score integer NOT NULL,
                Games integer NOT NULL,
                HighGame integer NOT NULL
            );
            CREATE TEMP TABLE Teams (
                Id integer PRIMARY KEY,
                TeamTournamentId integer NOT NULL,
                Forfeit boolean NOT NULL
            );
            CREATE TEMP TABLE TeamMember (
                Teams_Id integer NOT NULL,
                Bowlers_Id integer NOT NULL
            );
            CREATE TEMP TABLE SquadTeams (
                Id integer PRIMARY KEY,
                TeamSquadId integer NOT NULL,
                TeamId integer NOT NULL,
                HighGame integer NOT NULL
            );
            """;
        await createStats.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _legacyConnection.DisposeAsync();
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
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

    private async Task InsertResultsStatsAsync(int statsId, int? place, decimal payout, int points)
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = "INSERT INTO Stats_ResultsStats (Id, Place, Payout, Points) VALUES (@Id, @Place, @Payout, @Points)";
        insert.Parameters.AddWithValue("@Id", statsId);
        insert.Parameters.AddWithValue("@Place", place.HasValue ? place.Value : DBNull.Value);
        insert.Parameters.AddWithValue("@Payout", payout);
        insert.Parameters.AddWithValue("@Points", points);
        await insert.ExecuteNonQueryAsync();
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

    private async Task InsertTeamAsync(int teamId, int legacyTournamentId, bool forfeit)
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = "INSERT INTO Teams (Id, TeamTournamentId, Forfeit) VALUES (@Id, @TeamTournamentId, @Forfeit)";
        insert.Parameters.AddWithValue("@Id", teamId);
        insert.Parameters.AddWithValue("@TeamTournamentId", legacyTournamentId);
        insert.Parameters.AddWithValue("@Forfeit", forfeit);
        await insert.ExecuteNonQueryAsync();
    }

    private async Task InsertTeamMemberAsync(int teamId, int legacyBowlerId)
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = "INSERT INTO TeamMember (Teams_Id, Bowlers_Id) VALUES (@TeamId, @BowlerId)";
        insert.Parameters.AddWithValue("@TeamId", teamId);
        insert.Parameters.AddWithValue("@BowlerId", legacyBowlerId);
        await insert.ExecuteNonQueryAsync();
    }

    private async Task InsertSquadTeamAsync(int teamId, int squadId, int highGame)
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = "INSERT INTO SquadTeams (Id, TeamSquadId, TeamId, HighGame) VALUES (@Id, @SquadId, @TeamId, @HighGame)";
        insert.Parameters.AddWithValue("@Id", _nextSquadTeamsId++);
        insert.Parameters.AddWithValue("@SquadId", squadId);
        insert.Parameters.AddWithValue("@TeamId", teamId);
        insert.Parameters.AddWithValue("@HighGame", highGame);
        await insert.ExecuteNonQueryAsync();
    }

    private async Task<Tournament> CreateTournamentAsync(int legacyTournamentId, TournamentType? tournamentType, bool complete, CancellationToken ct)
    {
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id, legacyId: legacyTournamentId, tournamentType: tournamentType);
        if (complete)
        {
            tournament.CompleteTournament();
        }

        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        return tournament;
    }

    private async Task<Bowler> CreateBowlerAsync(int legacyBowlerId, CancellationToken ct)
    {
        var bowler = BowlerFactory.Create(legacyId: legacyBowlerId);
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        return bowler;
    }

    private SyncTournamentResultsJob CreateJob(Mock<IEmailSender>? emailSender = null, FakeLogger<SyncTournamentResultsJob>? logger = null) =>
        new(_dbContext, _legacyConnection, (emailSender ?? new Mock<IEmailSender>(MockBehavior.Strict)).Object, logger ?? new FakeLogger<SyncTournamentResultsJob>());

    private async Task<Tournament> ReloadTournamentAsync(int legacyTournamentId, CancellationToken ct) =>
        await _dbContext.Tournaments.Include(t => t.Results).SingleAsync(t => t.LegacyId == legacyTournamentId, ct);

    [Fact(DisplayName = "SyncAsync should log a warning and make no changes when the legacy tournament is not synced")]
    public async Task SyncAsync_ShouldLogWarningAndMakeNoChanges_WhenTournamentNotSynced()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fakeLogger = new FakeLogger<SyncTournamentResultsJob>();
        var job = CreateJob(logger: fakeLogger);

        // Act - Strict email mock: any SendAsync call without a setup would throw.
        await job.SyncAsync(999, ct);

        // Assert
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("999");
    }

    [Fact(DisplayName = "SyncAsync should record a singles result for an already-placed bowler and fill a missing place from qualifying stats")]
    public async Task SyncAsync_ShouldRecordAlreadyPlacedAndFillMissingSinglesPlace()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await CreateTournamentAsync(42, TournamentType.Singles, complete: true, ct);
        var placedBowler = await CreateBowlerAsync(100, ct);
        var cutBowler = await CreateBowlerAsync(200, ct);

        var placedStatsId = await InsertStatsAsync(100, 42);
        await InsertResultsStatsAsync(placedStatsId, place: 1, payout: 500m, points: 100);
        await InsertQualifyingStatsAsync(placedStatsId, squadId: 1, score: 1400, games: 6, highGame: 250);

        var cutStatsId = await InsertStatsAsync(200, 42);
        await InsertResultsStatsAsync(cutStatsId, place: null, payout: 0m, points: 50);
        await InsertQualifyingStatsAsync(cutStatsId, squadId: 1, score: 1100, games: 6, highGame: 200);

        var job = CreateJob();

        // Act
        await job.SyncAsync(42, ct);

        // Assert
        var tournament = await ReloadTournamentAsync(42, ct);
        var placedResult = tournament.Results.Single(r => r.BowlerId == placedBowler.Id);
        placedResult.Place.ShouldBe(1);
        placedResult.PrizeMoney.ShouldBe(500m);
        placedResult.Points.ShouldBe(100);

        var cutResult = tournament.Results.Single(r => r.BowlerId == cutBowler.Id);
        cutResult.Place.ShouldBe(2);
        cutResult.PrizeMoney.ShouldBe(0m);
        cutResult.Points.ShouldBe(50);
    }

    [Fact(DisplayName = "SyncAsync should place a non-advancing team roster together, excluding a forfeited roster")]
    public async Task SyncAsync_ShouldPlaceNonAdvancingRosterTogether_ExcludingForfeitedRoster()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await CreateTournamentAsync(42, TournamentType.Doubles, complete: true, ct);
        var advancedBowler = await CreateBowlerAsync(100, ct);
        var cutBowlerA = await CreateBowlerAsync(200, ct);
        var cutBowlerB = await CreateBowlerAsync(201, ct);
        var forfeitedBowler = await CreateBowlerAsync(300, ct);

        var advancedStatsId = await InsertStatsAsync(100, 42);
        await InsertResultsStatsAsync(advancedStatsId, place: 1, payout: 1000m, points: 200);

        var cutAStatsId = await InsertStatsAsync(200, 42);
        await InsertResultsStatsAsync(cutAStatsId, place: null, payout: 0m, points: 50);
        await InsertQualifyingStatsAsync(cutAStatsId, squadId: 1, score: 900, games: 6, highGame: 180);

        var cutBStatsId = await InsertStatsAsync(201, 42);
        await InsertResultsStatsAsync(cutBStatsId, place: null, payout: 0m, points: 50);
        await InsertQualifyingStatsAsync(cutBStatsId, squadId: 1, score: 850, games: 6, highGame: 170);

        var forfeitStatsId = await InsertStatsAsync(300, 42);
        await InsertResultsStatsAsync(forfeitStatsId, place: null, payout: 0m, points: 50);
        // Highest raw score of anyone missing a place - must still lose to the cut roster because it's forfeited.
        await InsertQualifyingStatsAsync(forfeitStatsId, squadId: 1, score: 9999, games: 6, highGame: 300);

        await InsertTeamAsync(teamId: 1000, legacyTournamentId: 42, forfeit: false);
        await InsertTeamMemberAsync(teamId: 1000, legacyBowlerId: 200);
        await InsertTeamMemberAsync(teamId: 1000, legacyBowlerId: 201);
        await InsertSquadTeamAsync(teamId: 1000, squadId: 1, highGame: 190);

        await InsertTeamAsync(teamId: 2000, legacyTournamentId: 42, forfeit: true);
        await InsertTeamMemberAsync(teamId: 2000, legacyBowlerId: 300);
        await InsertSquadTeamAsync(teamId: 2000, squadId: 1, highGame: 300);

        var job = CreateJob();

        // Act
        await job.SyncAsync(42, ct);

        // Assert
        var tournament = await ReloadTournamentAsync(42, ct);
        tournament.Results.Single(r => r.BowlerId == advancedBowler.Id).Place.ShouldBe(1);

        var cutAResult = tournament.Results.Single(r => r.BowlerId == cutBowlerA.Id);
        var cutBResult = tournament.Results.Single(r => r.BowlerId == cutBowlerB.Id);
        cutAResult.Place.ShouldBe(2);
        cutBResult.Place.ShouldBe(2);

        // The forfeited bowler's roster contributes no ranked entry - they land past the cut roster.
        tournament.Results.Single(r => r.BowlerId == forfeitedBowler.Id).Place.ShouldBe(3);
    }

    [Fact(DisplayName = "SyncAsync should reduce a roster's re-entry across two SquadTeams rows to its best composite entry")]
    public async Task SyncAsync_ShouldReduceRosterReentry_ToBestCompositeEntry()
    {
        // Arrange - roster {A,B} re-entered: squad 1 composite worse than squad 2 composite.
        var ct = TestContext.Current.CancellationToken;
        await CreateTournamentAsync(42, TournamentType.Doubles, complete: true, ct);
        var bowlerA = await CreateBowlerAsync(100, ct);
        var bowlerB = await CreateBowlerAsync(101, ct);
        var otherBowlerC = await CreateBowlerAsync(200, ct);
        var otherBowlerD = await CreateBowlerAsync(201, ct);

        var aStatsSquad1 = await InsertStatsAsync(100, 42);
        await InsertResultsStatsAsync(aStatsSquad1, place: null, payout: 0m, points: 50);
        await InsertQualifyingStatsAsync(aStatsSquad1, squadId: 1, score: 500, games: 3, highGame: 180);

        var aStatsSquad2 = await InsertStatsAsync(100, 42);
        await InsertQualifyingStatsAsync(aStatsSquad2, squadId: 2, score: 700, games: 3, highGame: 240);

        var bStatsSquad1 = await InsertStatsAsync(101, 42);
        await InsertResultsStatsAsync(bStatsSquad1, place: null, payout: 0m, points: 50);
        await InsertQualifyingStatsAsync(bStatsSquad1, squadId: 1, score: 480, games: 3, highGame: 170);

        var bStatsSquad2 = await InsertStatsAsync(101, 42);
        await InsertQualifyingStatsAsync(bStatsSquad2, squadId: 2, score: 650, games: 3, highGame: 230);

        // Lower-scoring competing roster so the winning squad's higher rank is observable.
        var cStats = await InsertStatsAsync(200, 42);
        await InsertResultsStatsAsync(cStats, place: null, payout: 0m, points: 50);
        await InsertQualifyingStatsAsync(cStats, squadId: 1, score: 100, games: 3, highGame: 60);
        var dStats = await InsertStatsAsync(201, 42);
        await InsertResultsStatsAsync(dStats, place: null, payout: 0m, points: 50);
        await InsertQualifyingStatsAsync(dStats, squadId: 1, score: 100, games: 3, highGame: 60);

        await InsertTeamAsync(teamId: 1000, legacyTournamentId: 42, forfeit: false);
        await InsertTeamMemberAsync(1000, 100);
        await InsertTeamMemberAsync(1000, 101);
        await InsertSquadTeamAsync(1000, squadId: 1, highGame: 185);
        await InsertSquadTeamAsync(1000, squadId: 2, highGame: 245); // this is the counting entry's HighGame

        await InsertTeamAsync(teamId: 2000, legacyTournamentId: 42, forfeit: false);
        await InsertTeamMemberAsync(2000, 200);
        await InsertTeamMemberAsync(2000, 201);
        await InsertSquadTeamAsync(2000, squadId: 1, highGame: 65);

        var job = CreateJob();

        // Act
        await job.SyncAsync(42, ct);

        // Assert - roster {A,B}'s squad-2 composite (1350) outranks roster {C,D}'s (200)
        var tournament = await ReloadTournamentAsync(42, ct);
        tournament.Results.Single(r => r.BowlerId == bowlerA.Id).Place.ShouldBe(1);
        tournament.Results.Single(r => r.BowlerId == bowlerB.Id).Place.ShouldBe(1);
        tournament.Results.Single(r => r.BowlerId == otherBowlerC.Id).Place.ShouldBe(2);
        tournament.Results.Single(r => r.BowlerId == otherBowlerD.Id).Place.ShouldBe(2);
    }

    [Fact(DisplayName = "SyncAsync should skip an unmapped bowler, log a warning, and send an email flagging the team-tournament caveat")]
    public async Task SyncAsync_ShouldSkipUnmappedBowlerAndSendTeamAwareEmail_WhenBowlerHasNoWebsiteMatch()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await CreateTournamentAsync(42, TournamentType.Doubles, complete: true, ct);

        var unmappedStatsId = await InsertStatsAsync(999, 42);
        await InsertResultsStatsAsync(unmappedStatsId, place: 1, payout: 500m, points: 100);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        EmailMessage? sentMessage = null;
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => sentMessage = message)
            .Returns(Task.CompletedTask);

        var fakeLogger = new FakeLogger<SyncTournamentResultsJob>();
        var job = CreateJob(emailSender, fakeLogger);

        // Act
        await job.SyncAsync(42, ct);

        // Assert - Strict mock: the Setup above is the verification that SendAsync was called.
        sentMessage.ShouldNotBeNull();
        sentMessage.HtmlBody.ShouldContain("999");
        sentMessage.HtmlBody.ShouldContain("team tournament", Case.Insensitive);
        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Warning && r.Message.Contains("999", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "SyncAsync should skip a bowler with no Place and no qualifying stats, logging a warning")]
    public async Task SyncAsync_ShouldSkipUnplaceableBowler_WhenNoPlaceAndNoQualifyingStats()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await CreateTournamentAsync(42, TournamentType.Singles, complete: true, ct);
        await CreateBowlerAsync(100, ct);

        var statsId = await InsertStatsAsync(100, 42);
        await InsertResultsStatsAsync(statsId, place: null, payout: 0m, points: 50); // no-show: no qualifying row at all

        var fakeLogger = new FakeLogger<SyncTournamentResultsJob>();
        var job = CreateJob(logger: fakeLogger);

        // Act
        await job.SyncAsync(42, ct);

        // Assert
        var tournament = await ReloadTournamentAsync(42, ct);
        tournament.Results.ShouldBeEmpty();
        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Warning && r.Message.Contains("100", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "SyncAsync should log an error and skip a bowler with more than one Stats_ResultsStats row, without throwing")]
    public async Task SyncAsync_ShouldLogErrorAndSkip_WhenBowlerHasMultipleResultRows()
    {
        // Arrange - data anomaly: two Stats rows for the same bowler, each with its own ResultsStats row.
        var ct = TestContext.Current.CancellationToken;
        await CreateTournamentAsync(42, TournamentType.Singles, complete: true, ct);
        await CreateBowlerAsync(100, ct);

        var firstStatsId = await InsertStatsAsync(100, 42);
        await InsertResultsStatsAsync(firstStatsId, place: 1, payout: 500m, points: 100);
        var secondStatsId = await InsertStatsAsync(100, 42);
        await InsertResultsStatsAsync(secondStatsId, place: 2, payout: 300m, points: 90);

        var fakeLogger = new FakeLogger<SyncTournamentResultsJob>();
        var job = CreateJob(logger: fakeLogger);

        // Act & Assert
        await Should.NotThrowAsync(() => job.SyncAsync(42, ct));

        var tournament = await ReloadTournamentAsync(42, ct);
        tournament.Results.ShouldBeEmpty();
        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Error && r.Message.Contains("100", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "SyncAsync should query only the rows for the requested legacy tournament id")]
    public async Task SyncAsync_ShouldQueryOnlyRowsForRequestedTournamentId()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await CreateTournamentAsync(42, TournamentType.Singles, complete: true, ct);
        await CreateTournamentAsync(43, TournamentType.Singles, complete: true, ct);
        var bowler = await CreateBowlerAsync(100, ct);
        await CreateBowlerAsync(200, ct);

        var statsId = await InsertStatsAsync(100, 42);
        await InsertResultsStatsAsync(statsId, place: 1, payout: 500m, points: 100);

        var otherTournamentStatsId = await InsertStatsAsync(200, 43);
        await InsertResultsStatsAsync(otherTournamentStatsId, place: 1, payout: 500m, points: 100);

        var job = CreateJob();

        // Act
        await job.SyncAsync(42, ct);

        // Assert
        var tournament = await ReloadTournamentAsync(42, ct);
        var result = tournament.Results.ShouldHaveSingleItem();
        result.BowlerId.ShouldBe(bowler.Id);

        var otherTournament = await ReloadTournamentAsync(43, ct);
        otherTournament.Results.ShouldBeEmpty();
    }

    [Fact(DisplayName = "SyncAsync should not duplicate results and should log the skip when run twice for the same legacy tournament id")]
    public async Task SyncAsync_ShouldNotDuplicateResults_WhenRunTwice()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await CreateTournamentAsync(42, TournamentType.Singles, complete: true, ct);
        var bowler = await CreateBowlerAsync(100, ct);

        var statsId = await InsertStatsAsync(100, 42);
        await InsertResultsStatsAsync(statsId, place: 1, payout: 500m, points: 100);

        var fakeLogger = new FakeLogger<SyncTournamentResultsJob>();
        var job = CreateJob(logger: fakeLogger);

        // Act
        await job.SyncAsync(42, ct);
        _dbContext.ChangeTracker.Clear();
        await job.SyncAsync(42, ct);

        // Assert
        var tournament = await ReloadTournamentAsync(42, ct);
        var result = tournament.Results.ShouldHaveSingleItem();
        result.BowlerId.ShouldBe(bowler.Id);
        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Information);
    }
}
