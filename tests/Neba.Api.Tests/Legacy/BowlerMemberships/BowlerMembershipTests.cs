using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;

using FluentValidation;

using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

using Neba.Api.Contracts.Memberships;
using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Memberships.Domain;
using Neba.Api.Legacy;
using Neba.Api.Legacy.BowlerMemberships;
using Neba.Api.Legacy.Bowlers;
using Neba.Api.Legacy.HallOfFame;
using Neba.Api.Legacy.Seasons.Complete;
using Neba.Api.Legacy.Tournaments;
using Neba.Api.Legacy.Tournaments.Complete;
using Neba.Api.Legacy.Tournaments.Stats;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Legacy.BowlerMemberships;

// Mirrors the layout of the production folder (Legacy/BowlerMemberships/) so the whole test surface
// for these backdoor actions is removed alongside it at sunset. The weekly step has its own file
// next to the other weekly-sync tests (Legacy/Sync/BowlerMembershipWeeklySyncTests.cs).

[UnitTest]
[Component("Legacy")]
public sealed class BowlerMembershipRequestValidatorTests
{
    [Fact(DisplayName = "NewBowlerMembershipRequestValidator should succeed when BowlerMembershipId is greater than zero")]
    public void NewValidator_ShouldSucceed_WhenIdIsPositive()
    {
        // Arrange
        var validator = new NewBowlerMembershipRequestValidator();

        // Act
        var result = validator.Validate(new NewBowlerMembershipRequest(1));

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "NewBowlerMembershipRequestValidator should fail when BowlerMembershipId is not greater than zero")]
    [InlineData(0, TestDisplayName = "Zero")]
    [InlineData(-1, TestDisplayName = "Negative")]
    public void NewValidator_ShouldFail_WhenIdIsNotPositive(int id)
    {
        // Arrange
        var validator = new NewBowlerMembershipRequestValidator();

        // Act
        var result = validator.Validate(new NewBowlerMembershipRequest(id));

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NewBowlerMembershipRequest.BowlerMembershipId));
    }

    [Fact(DisplayName = "UpdateBowlerMembershipRequestValidator should succeed when BowlerMembershipId is greater than zero")]
    public void UpdateValidator_ShouldSucceed_WhenIdIsPositive()
    {
        // Arrange
        var validator = new UpdateBowlerMembershipRequestValidator();

        // Act
        var result = validator.Validate(new UpdateBowlerMembershipRequest(1));

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "UpdateBowlerMembershipRequestValidator should fail when BowlerMembershipId is not greater than zero")]
    [InlineData(0, TestDisplayName = "Zero")]
    [InlineData(-1, TestDisplayName = "Negative")]
    public void UpdateValidator_ShouldFail_WhenIdIsNotPositive(int id)
    {
        // Arrange
        var validator = new UpdateBowlerMembershipRequestValidator();

        // Act
        var result = validator.Validate(new UpdateBowlerMembershipRequest(id));

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateBowlerMembershipRequest.BowlerMembershipId));
    }

    [Fact(DisplayName = "DeleteBowlerMembershipRequestValidator should succeed when BowlerMembershipId is greater than zero")]
    public void DeleteValidator_ShouldSucceed_WhenIdIsPositive()
    {
        // Arrange
        var validator = new DeleteBowlerMembershipRequestValidator();

        // Act
        var result = validator.Validate(new DeleteBowlerMembershipRequest(1));

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "DeleteBowlerMembershipRequestValidator should fail when BowlerMembershipId is not greater than zero")]
    [InlineData(0, TestDisplayName = "Zero")]
    [InlineData(-1, TestDisplayName = "Negative")]
    public void DeleteValidator_ShouldFail_WhenIdIsNotPositive(int id)
    {
        // Arrange
        var validator = new DeleteBowlerMembershipRequestValidator();

        // Act
        var result = validator.Validate(new DeleteBowlerMembershipRequest(id));

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DeleteBowlerMembershipRequest.BowlerMembershipId));
    }
}

[UnitTest]
[Component("Legacy")]
public sealed class LegacyBowlerMembershipSyncApplyTests
{
    private static readonly MembershipType TypeA = MembershipType.FromLegacyId(1);
    private static readonly MembershipType TypeB = MembershipType.FromLegacyId(2);

    private static LegacyBowlerMembershipRow Row(
        int id = 10,
        DateTime? begin = null,
        DateTime? end = null,
        decimal cost = 50m)
        => new(id, 1, 1, begin ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), end ?? new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Unspecified), cost);

    private static Bowler BowlerWithMembership(int legacyId = 10)
    {
        var bowler = BowlerFactory.Create(legacyId: 1);
        bowler.AddLegacyMembership(legacyId, TypeA, 50m, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)).IsError.ShouldBeFalse();
        return bowler;
    }

    [Fact(DisplayName = "Apply should add the membership and return Created when the bowler has no membership with that legacy id")]
    public void Apply_ShouldCreate_WhenNoMembershipHasThatLegacyId()
    {
        // Arrange
        var bowler = BowlerFactory.Create(legacyId: 1);

        // Act
        var (outcome, errors) = LegacyBowlerMembershipSync.Apply(bowler, Row(), TypeA);

        // Assert
        outcome.ShouldBe(LegacyMembershipOutcome.Created);
        errors.ShouldBeNull();
        var membership = bowler.Memberships.ShouldHaveSingleItem();
        membership.LegacyId.ShouldBe(10);
        membership.Type.ShouldBe(TypeA);
        membership.Cost.ShouldBe(50m);
        membership.StartDate.ShouldBe(new DateOnly(2026, 1, 1));
        membership.EndDate.ShouldBe(new DateOnly(2026, 12, 31));
    }

    [Fact(DisplayName = "Apply should return Unchanged and write nothing when every field already matches")]
    public void Apply_ShouldReturnUnchanged_WhenEveryFieldMatches()
    {
        // Arrange
        var bowler = BowlerWithMembership();

        // Act
        var (outcome, errors) = LegacyBowlerMembershipSync.Apply(bowler, Row(), TypeA);

        // Assert
        outcome.ShouldBe(LegacyMembershipOutcome.Unchanged);
        errors.ShouldBeNull();
        bowler.Memberships.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Apply should return Updated and change the type when only the type differs")]
    public void Apply_ShouldUpdate_WhenTypeDiffers()
    {
        // Arrange
        var bowler = BowlerWithMembership();

        // Act
        var (outcome, _) = LegacyBowlerMembershipSync.Apply(bowler, Row(), TypeB);

        // Assert
        outcome.ShouldBe(LegacyMembershipOutcome.Updated);
        bowler.Memberships.Single().Type.ShouldBe(TypeB);
    }

    [Fact(DisplayName = "Apply should return Updated and change the cost when only the cost differs")]
    public void Apply_ShouldUpdate_WhenCostDiffers()
    {
        // Arrange
        var bowler = BowlerWithMembership();

        // Act
        var (outcome, _) = LegacyBowlerMembershipSync.Apply(bowler, Row(cost: 60m), TypeA);

        // Assert
        outcome.ShouldBe(LegacyMembershipOutcome.Updated);
        bowler.Memberships.Single().Cost.ShouldBe(60m);
    }

    [Fact(DisplayName = "Apply should return Updated and change the start date when only the start date differs")]
    public void Apply_ShouldUpdate_WhenStartDateDiffers()
    {
        // Arrange
        var bowler = BowlerWithMembership();

        // Act
        var (outcome, _) = LegacyBowlerMembershipSync.Apply(bowler, Row(begin: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Unspecified)), TypeA);

        // Assert
        outcome.ShouldBe(LegacyMembershipOutcome.Updated);
        bowler.Memberships.Single().StartDate.ShouldBe(new DateOnly(2026, 2, 1));
    }

    [Fact(DisplayName = "Apply should return Updated and change the end date when only the end date differs")]
    public void Apply_ShouldUpdate_WhenEndDateDiffers()
    {
        // Arrange
        var bowler = BowlerWithMembership();

        // Act
        var (outcome, _) = LegacyBowlerMembershipSync.Apply(bowler, Row(end: new DateTime(2027, 1, 31, 0, 0, 0, DateTimeKind.Unspecified)), TypeA);

        // Assert
        outcome.ShouldBe(LegacyMembershipOutcome.Updated);
        bowler.Memberships.Single().EndDate.ShouldBe(new DateOnly(2027, 1, 31));
    }

    [Fact(DisplayName = "Apply should return Failed and keep the membership untouched when the new cost is negative")]
    public void Apply_ShouldFail_WhenCostIsNegative()
    {
        // Arrange
        var bowler = BowlerWithMembership();

        // Act
        var (outcome, errors) = LegacyBowlerMembershipSync.Apply(bowler, Row(cost: -1m), TypeA);

        // Assert
        outcome.ShouldBe(LegacyMembershipOutcome.Failed);
        errors.ShouldNotBeNull();
        bowler.Memberships.Single().Cost.ShouldBe(50m);
    }

    [Fact(DisplayName = "Apply should return Failed and keep the membership untouched when the new end date is before the start date")]
    public void Apply_ShouldFail_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        var bowler = BowlerWithMembership();

        // Act
        var (outcome, errors) = LegacyBowlerMembershipSync.Apply(
            bowler,
            Row(begin: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified), end: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Unspecified)),
            TypeA);

        // Assert
        outcome.ShouldBe(LegacyMembershipOutcome.Failed);
        errors.ShouldNotBeNull();
        bowler.Memberships.Single().StartDate.ShouldBe(new DateOnly(2026, 1, 1));
    }

    [Fact(DisplayName = "Apply should return Failed and add nothing when creating a membership with a negative cost")]
    public void Apply_ShouldFail_WhenCreatingWithNegativeCost()
    {
        // Arrange
        var bowler = BowlerFactory.Create(legacyId: 1);

        // Act
        var (outcome, errors) = LegacyBowlerMembershipSync.Apply(bowler, Row(cost: -1m), TypeA);

        // Assert
        outcome.ShouldBe(LegacyMembershipOutcome.Failed);
        errors.ShouldNotBeNull();
        bowler.Memberships.ShouldBeEmpty();
    }
}

[IntegrationTest]
[Component("Legacy")]
public sealed class BowlerMembershipEndpointTests : IAsyncLifetime
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
        builder.Services.AddScoped<IValidator<NewBowlerMembershipRequest>, NewBowlerMembershipRequestValidator>();
        builder.Services.AddScoped<IValidator<UpdateBowlerMembershipRequest>, UpdateBowlerMembershipRequestValidator>();
        builder.Services.AddScoped<IValidator<DeleteBowlerMembershipRequest>, DeleteBowlerMembershipRequestValidator>();
        // MapLegacyGroup() maps every endpoint in the group, and an unregistered sibling validator
        // throws when route metadata builds on the first request.
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
        _app.MapLegacyGroup();

        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    [Theory(DisplayName = "POST /legacy/bowler-memberships returns 401 and does not create a job when the X-Api-Key header is missing")]
    [InlineData("new")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task Post_ShouldReturn401AndNotCreateJob_WhenApiKeyHeaderIsMissing(string action)
    {
        // Arrange
        using var client = _app.GetTestClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            $"/legacy/bowler-memberships/{action}",
            new { bowlerMembershipId = 42 },
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was created.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory(DisplayName = "POST /legacy/bowler-memberships returns 401 and does not create a job when the X-Api-Key header is wrong")]
    [InlineData("new")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task Post_ShouldReturn401AndNotCreateJob_WhenApiKeyHeaderIsWrong(string action)
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        // Act
        using var response = await client.PostAsJsonAsync(
            $"/legacy/bowler-memberships/{action}",
            new { bowlerMembershipId = 42 },
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory(DisplayName = "POST /legacy/bowler-memberships returns 400 and does not create a job when BowlerMembershipId is invalid")]
    [InlineData("new")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task Post_ShouldReturn400AndNotCreateJob_WhenIdIsInvalid(string action)
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ValidApiKey);

        // Act
        using var response = await client.PostAsJsonAsync(
            $"/legacy/bowler-memberships/{action}",
            new { bowlerMembershipId = 0 },
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory(DisplayName = "POST /legacy/bowler-memberships/new and /update return 202 and schedule an UpsertAsync job after the settle delay")]
    [InlineData("new")]
    [InlineData("update")]
    public async Task Post_ShouldReturn202AndScheduleUpsert_WhenRequestIsValid(string action)
    {
        // Arrange
        Job? capturedJob = null;
        IState? capturedState = null;
        _jobsMock
            .Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, state) =>
            {
                capturedJob = job;
                capturedState = state;
            })
            .Returns("job-1");

        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ValidApiKey);

        // Act
        using var response = await client.PostAsJsonAsync(
            $"/legacy/bowler-memberships/{action}",
            new { bowlerMembershipId = 42 },
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        capturedJob.ShouldNotBeNull();
        capturedJob.Type.ShouldBe(typeof(BowlerMembershipSyncJob));
        capturedJob.Method.Name.ShouldBe(nameof(BowlerMembershipSyncJob.UpsertAsync));
        capturedJob.Args[0].ShouldBe(42);
        var scheduled = capturedState.ShouldBeOfType<ScheduledState>();
        var delay = scheduled.EnqueueAt - DateTime.UtcNow;
        delay.ShouldBeGreaterThan(BowlerMembershipSyncJob.BowlerSettleDelay - TimeSpan.FromSeconds(10));
        delay.ShouldBeLessThanOrEqualTo(BowlerMembershipSyncJob.BowlerSettleDelay);
    }

    [Fact(DisplayName = "POST /legacy/bowler-memberships/delete returns 202 and enqueues a DeleteAsync job with no delay")]
    public async Task Post_ShouldReturn202AndEnqueueDelete_WhenRequestIsValid()
    {
        // Arrange
        Job? capturedJob = null;
        IState? capturedState = null;
        _jobsMock
            .Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, state) =>
            {
                capturedJob = job;
                capturedState = state;
            })
            .Returns("job-1");

        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ValidApiKey);

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/bowler-memberships/delete",
            new { bowlerMembershipId = 42 },
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        capturedJob.ShouldNotBeNull();
        capturedJob.Type.ShouldBe(typeof(BowlerMembershipSyncJob));
        capturedJob.Method.Name.ShouldBe(nameof(BowlerMembershipSyncJob.DeleteAsync));
        capturedJob.Args[0].ShouldBe(42);
        capturedState.ShouldBeOfType<EnqueuedState>();
    }
}

[IntegrationTest]
[Component("Legacy")]
[Collection(nameof(LegacyDatabasesTestScope))]
public sealed class BowlerMembershipSyncJobTests(AppDbContextFixture fixture, LegacySqlServerFixture legacyFixture)
    : IClassFixture<AppDbContextFixture>, IClassFixture<LegacySqlServerFixture>, IAsyncLifetime
{
    private const int UnmappedMembershipId = 99;

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    // Ownership stays with LegacySqlServerFixture, which disposes it once for the whole run.
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed",
        Justification = "Ownership stays with LegacySqlServerFixture, which disposes it once for the whole run.")]
    private LegacySqlServerDatabase _legacyDatabase = null!;
    private ServiceProvider _serviceProvider = null!;

    private SqlConnection _legacyConnection => _legacyDatabase.Connection;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();

        var services = new ServiceCollection();
        services.AddFusionCache().WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromHours(1));
        _serviceProvider = services.BuildServiceProvider();

        _legacyDatabase = await legacyFixture.GetOrCreateDatabaseAsync(nameof(BowlerMembershipSyncJobTests), CreateSchemaAsync);
        await _legacyDatabase.ResetAsync();

        await InsertLegacyMembershipTypeAsync(1, 50m);
        await InsertLegacyMembershipTypeAsync(2, 75m);
        await InsertLegacyMembershipTypeAsync(LegacyBowlerMembershipSync.SystemImportMembershipId, 0m);
        await InsertLegacyMembershipTypeAsync(UnmappedMembershipId, 10m);
    }

    private static async Task CreateSchemaAsync(SqlConnection connection)
    {
        await using var create = connection.CreateCommand();
        create.CommandText = """
            CREATE TABLE Bowlers (
                Id int PRIMARY KEY,
                FirstName nvarchar(100) NOT NULL,
                MiddleInitial nvarchar(10) NULL,
                LastName nvarchar(100) NOT NULL,
                Suffix nvarchar(10) NULL,
                Gender int NOT NULL,
                DateOfBirth datetime NULL
            );
            CREATE TABLE Memberships (
                Id int PRIMARY KEY,
                Cost decimal(5,2) NOT NULL
            );
            CREATE TABLE BowlerMemberships (
                Id int PRIMARY KEY,
                BowlerId int NOT NULL,
                MembershipId int NOT NULL,
                BeginDate datetime NOT NULL,
                EndDate datetime NOT NULL
            );
            """;
        await create.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task InsertLegacyMembershipTypeAsync(int id, decimal cost)
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = "INSERT INTO Memberships (Id, Cost) VALUES (@Id, @Cost)";
        insert.Parameters.AddWithValue("@Id", id);
        insert.Parameters.AddWithValue("@Cost", cost);
        await insert.ExecuteNonQueryAsync();
    }

    private async Task InsertLegacyBowlerAsync(int id, string firstName = "David", string lastName = "Smith")
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = """
            INSERT INTO Bowlers (Id, FirstName, MiddleInitial, LastName, Suffix, Gender, DateOfBirth)
            VALUES (@Id, @FirstName, NULL, @LastName, NULL, 0, NULL)
            """;
        insert.Parameters.AddWithValue("@Id", id);
        insert.Parameters.AddWithValue("@FirstName", firstName);
        insert.Parameters.AddWithValue("@LastName", lastName);
        await insert.ExecuteNonQueryAsync();
    }

    private async Task InsertLegacyMembershipAsync(
        int id,
        int bowlerId = 1,
        int membershipId = 1,
        DateTime? begin = null,
        DateTime? end = null)
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = """
            INSERT INTO BowlerMemberships (Id, BowlerId, MembershipId, BeginDate, EndDate)
            VALUES (@Id, @BowlerId, @MembershipId, @BeginDate, @EndDate)
            """;
        insert.Parameters.AddWithValue("@Id", id);
        insert.Parameters.AddWithValue("@BowlerId", bowlerId);
        insert.Parameters.AddWithValue("@MembershipId", membershipId);
        insert.Parameters.AddWithValue("@BeginDate", begin ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));
        insert.Parameters.AddWithValue("@EndDate", end ?? new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Unspecified));
        await insert.ExecuteNonQueryAsync();
    }

    private async Task UpdateLegacyMembershipAsync(int id, int membershipId, DateTime begin, DateTime end)
    {
        await using var update = _legacyConnection.CreateCommand();
        update.CommandText = "UPDATE BowlerMemberships SET MembershipId = @MembershipId, BeginDate = @BeginDate, EndDate = @EndDate WHERE Id = @Id";
        update.Parameters.AddWithValue("@Id", id);
        update.Parameters.AddWithValue("@MembershipId", membershipId);
        update.Parameters.AddWithValue("@BeginDate", begin);
        update.Parameters.AddWithValue("@EndDate", end);
        await update.ExecuteNonQueryAsync();
    }

    private async Task DeleteLegacyMembershipAsync(int id)
    {
        await using var delete = _legacyConnection.CreateCommand();
        delete.CommandText = "DELETE FROM BowlerMemberships WHERE Id = @Id";
        delete.Parameters.AddWithValue("@Id", id);
        await delete.ExecuteNonQueryAsync();
    }

    private async Task SeedWebsiteBowlerAsync(int legacyId)
    {
        var bowler = BowlerFactory.Create(legacyId: legacyId);
        await _dbContext.Set<Bowler>().AddAsync(bowler, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        _dbContext.ChangeTracker.Clear();
    }

    private async Task SeedWebsiteMembershipAsync(int bowlerLegacyId, int membershipLegacyId)
    {
        var bowler = BowlerFactory.Create(legacyId: bowlerLegacyId);
        bowler.AddLegacyMembership(
            membershipLegacyId,
            MembershipType.FromLegacyId(1),
            50m,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31)).IsError.ShouldBeFalse();
        await _dbContext.Set<Bowler>().AddAsync(bowler, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        _dbContext.ChangeTracker.Clear();
    }

    private BowlerMembershipSyncJob CreateJob(FakeLogger<BowlerMembershipSyncJob>? logger = null) =>
        new(
            _dbContext,
            _legacyConnection,
            _serviceProvider.GetRequiredService<IFusionCache>(),
            logger ?? new FakeLogger<BowlerMembershipSyncJob>());

    private async Task<List<BowlerMembership>> WebsiteMembershipsAsync()
        => await _dbContext.Set<BowlerMembership>().AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);

    private async Task CacheBowlersEntryAsync()
    {
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        await cache.GetOrSetAsync("membership-sync-bowlers", _ => Task.FromResult("stale"), tags: ["neba:bowlers"], token: TestContext.Current.CancellationToken);
    }

    private async Task<string> ReadBowlersEntryAsync()
    {
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        return await cache.GetOrSetAsync("membership-sync-bowlers", _ => Task.FromResult("fresh"), token: TestContext.Current.CancellationToken);
    }

    [Fact(DisplayName = "UpsertAsync should create the membership with the Software's id and joined cost when the bowler already exists")]
    public async Task UpsertAsync_ShouldCreateMembership_WhenBowlerExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(10, bowlerId: 1, membershipId: 2, begin: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Unspecified), end: new DateTime(2027, 2, 28, 0, 0, 0, DateTimeKind.Unspecified));
        await CacheBowlersEntryAsync();
        var job = CreateJob();

        // Act
        await job.UpsertAsync(10, ct);

        // Assert
        var membership = (await WebsiteMembershipsAsync()).ShouldHaveSingleItem();
        membership.LegacyId.ShouldBe(10);
        membership.Type.ShouldBe(MembershipType.FromLegacyId(2));
        membership.Cost.ShouldBe(75m);
        membership.StartDate.ShouldBe(new DateOnly(2026, 3, 1));
        membership.EndDate.ShouldBe(new DateOnly(2027, 2, 28));
        (await ReadBowlersEntryAsync()).ShouldBe("stale");
    }

    [Fact(DisplayName = "UpsertAsync should be idempotent: a second run keeps one row and writes nothing")]
    public async Task UpsertAsync_ShouldBeIdempotent_WhenRunTwice()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(10);
        var job = CreateJob();
        await job.UpsertAsync(10, ct);
        var first = (await WebsiteMembershipsAsync()).Single();
        _dbContext.ChangeTracker.Clear();

        // Act
        await job.UpsertAsync(10, ct);

        // Assert
        var memberships = await WebsiteMembershipsAsync();
        memberships.ShouldHaveSingleItem().Id.ShouldBe(first.Id);
        _dbContext.ChangeTracker.HasChanges().ShouldBeFalse();
    }

    [Fact(DisplayName = "UpsertAsync should update the website membership when the Software changed its type and dates")]
    public async Task UpsertAsync_ShouldUpdateMembership_WhenSoftwareRowChanged()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(10, membershipId: 1);
        var job = CreateJob();
        await job.UpsertAsync(10, ct);
        _dbContext.ChangeTracker.Clear();
        await UpdateLegacyMembershipAsync(10, 2, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2027, 5, 31, 0, 0, 0, DateTimeKind.Unspecified));

        // Act
        await job.UpsertAsync(10, ct);

        // Assert
        var membership = (await WebsiteMembershipsAsync()).ShouldHaveSingleItem();
        membership.Type.ShouldBe(MembershipType.FromLegacyId(2));
        membership.Cost.ShouldBe(75m);
        membership.StartDate.ShouldBe(new DateOnly(2026, 6, 1));
        membership.EndDate.ShouldBe(new DateOnly(2027, 5, 31));
    }

    [Fact(DisplayName = "UpsertAsync should write nothing and log a warning when the Software row is gone")]
    public async Task UpsertAsync_ShouldLogWarningAndWriteNothing_WhenSoftwareRowIsMissing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1);
        var logger = new FakeLogger<BowlerMembershipSyncJob>();
        var job = CreateJob(logger);

        // Act
        await job.UpsertAsync(999, ct);

        // Assert
        (await WebsiteMembershipsAsync()).ShouldBeEmpty();
        var record = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("999");
    }

    [Fact(DisplayName = "UpsertAsync should skip and log a System Import membership")]
    public async Task UpsertAsync_ShouldSkipAndLog_WhenMembershipIsSystemImport()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(10, membershipId: LegacyBowlerMembershipSync.SystemImportMembershipId, begin: new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), end: new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));
        var logger = new FakeLogger<BowlerMembershipSyncJob>();
        var job = CreateJob(logger);

        // Act
        await job.UpsertAsync(10, ct);

        // Assert
        (await WebsiteMembershipsAsync()).ShouldBeEmpty();
        var record = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldContain("System Import");
    }

    [Fact(DisplayName = "UpsertAsync should write nothing and log an error when the membership type maps to no MembershipType")]
    public async Task UpsertAsync_ShouldLogErrorAndWriteNothing_WhenTypeIsUnmapped()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(10, membershipId: UnmappedMembershipId);
        var logger = new FakeLogger<BowlerMembershipSyncJob>();
        var job = CreateJob(logger);

        // Act
        await job.UpsertAsync(10, ct);

        // Assert
        (await WebsiteMembershipsAsync()).ShouldBeEmpty();
        var record = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Error);
        record.Message.ShouldContain(UnmappedMembershipId.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact(DisplayName = "UpsertAsync should create the bowler from the Software, add the membership and evict the bowlers cache tag when the bowler is missing")]
    public async Task UpsertAsync_ShouldCreateBowlerAndEvict_WhenBowlerIsMissing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(1, firstName: "William", lastName: "Jones");
        await InsertLegacyMembershipAsync(10);
        await CacheBowlersEntryAsync();
        var logger = new FakeLogger<BowlerMembershipSyncJob>();
        var job = CreateJob(logger);

        // Act
        await job.UpsertAsync(10, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().Include(b => b.Memberships).SingleAsync(b => b.LegacyId == 1, ct);
        bowler.Name.LastName.ShouldBe("Jones");
        bowler.Memberships.ShouldHaveSingleItem().LegacyId.ShouldBe(10);
        (await ReadBowlersEntryAsync()).ShouldBe("fresh");
        logger.Collector.GetSnapshot().ShouldContain(record =>
            record.Level == LogLevel.Information && record.Message.Contains("created it", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "UpsertAsync should write nothing and log a warning when neither the website nor the Software has the bowler")]
    public async Task UpsertAsync_ShouldLogWarningAndWriteNothing_WhenBowlerIsMissingEverywhere()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyMembershipAsync(10, bowlerId: 7);
        var logger = new FakeLogger<BowlerMembershipSyncJob>();
        var job = CreateJob(logger);

        // Act
        await job.UpsertAsync(10, ct);

        // Assert
        (await WebsiteMembershipsAsync()).ShouldBeEmpty();
        (await _dbContext.Set<Bowler>().AnyAsync(ct)).ShouldBeFalse();
        var record = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("7");
    }

    [Fact(DisplayName = "UpsertAsync should leave one bowler and one membership when the membership job runs before NewBowlerSyncJob")]
    public async Task UpsertAsync_ShouldConverge_WhenMembershipJobRunsBeforeNewBowlerJob()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(10);
        await CreateJob().UpsertAsync(10, ct);
        _dbContext.ChangeTracker.Clear();
        var newBowlerJob = new NewBowlerSyncJob(_dbContext, _legacyConnection, new FakeLogger<NewBowlerSyncJob>());

        // Act
        await newBowlerJob.SyncAsync(1, ct);

        // Assert
        (await _dbContext.Set<Bowler>().CountAsync(ct)).ShouldBe(1);
        (await WebsiteMembershipsAsync()).ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "UpsertAsync should add the membership to the existing bowler when NewBowlerSyncJob runs first")]
    public async Task UpsertAsync_ShouldConverge_WhenNewBowlerJobRunsBeforeMembershipJob()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(10);
        await new NewBowlerSyncJob(_dbContext, _legacyConnection, new FakeLogger<NewBowlerSyncJob>()).SyncAsync(1, ct);
        _dbContext.ChangeTracker.Clear();

        // Act
        await CreateJob().UpsertAsync(10, ct);

        // Assert
        (await _dbContext.Set<Bowler>().CountAsync(ct)).ShouldBe(1);
        (await WebsiteMembershipsAsync()).ShouldHaveSingleItem().LegacyId.ShouldBe(10);
    }

    [Fact(DisplayName = "DeleteAsync should remove the website membership with that legacy id and leave the bowler")]
    public async Task DeleteAsync_ShouldRemoveMembership_WhenItExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteMembershipAsync(bowlerLegacyId: 1, membershipLegacyId: 10);
        var job = CreateJob();

        // Act
        await job.DeleteAsync(10, ct);

        // Assert
        (await WebsiteMembershipsAsync()).ShouldBeEmpty();
        (await _dbContext.Set<Bowler>().CountAsync(ct)).ShouldBe(1);
    }

    [Fact(DisplayName = "DeleteAsync should do nothing and log when no website membership has that legacy id")]
    public async Task DeleteAsync_ShouldLogAndDoNothing_WhenMembershipIsMissing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteMembershipAsync(bowlerLegacyId: 1, membershipLegacyId: 10);
        var logger = new FakeLogger<BowlerMembershipSyncJob>();
        var job = CreateJob(logger);

        // Act
        await job.DeleteAsync(999, ct);

        // Assert
        (await WebsiteMembershipsAsync()).ShouldHaveSingleItem();
        var record = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldContain("999");
    }

    [Fact(DisplayName = "UpsertAsync should add nothing when a delete beat the add and the Software row is gone")]
    public async Task UpsertAsync_ShouldAddNothing_WhenDeleteBeatTheAdd()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(10);
        await DeleteLegacyMembershipAsync(10);
        var job = CreateJob();

        // Act
        await job.DeleteAsync(10, ct);
        await job.UpsertAsync(10, ct);

        // Assert
        (await WebsiteMembershipsAsync()).ShouldBeEmpty();
    }
}
