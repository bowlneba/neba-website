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
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Legacy;
using Neba.Api.Legacy.Bowlers;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;

using Npgsql;

namespace Neba.Api.Tests.Legacy.Bowlers;

// Mirrors the single-file shape of the production source (Legacy/Bowlers/NewBowler.cs) so the
// whole test suite for this backdoor action is removed alongside it at sunset with no leftover
// test files to hunt down.

[UnitTest]
[Component("Legacy")]
public sealed class NewBowlerRequestValidatorTests
{
    private readonly NewBowlerRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when BowlerId is greater than zero")]
    public void Validate_ShouldSucceed_WhenBowlerIdIsGreaterThanZero()
    {
        // Arrange
        var request = new NewBowlerRequest(1);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail when BowlerId is not greater than zero")]
    [InlineData(0, TestDisplayName = "Zero")]
    [InlineData(-1, TestDisplayName = "Negative")]
    public void Validate_ShouldFail_WhenBowlerIdIsNotGreaterThanZero(int bowlerId)
    {
        // Arrange
        var request = new NewBowlerRequest(bowlerId);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NewBowlerRequest.BowlerId));
    }
}

[IntegrationTest]
[Component("Legacy")]
public sealed class NewBowlerEndpointTests : IAsyncLifetime
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
        builder.Services.AddScoped<IValidator<NewBowlerRequest>, NewBowlerRequestValidator>();
        builder.Services.AddSingleton(Options.Create(new LegacySettings { ApiKey = ValidApiKey }));

        _app = builder.Build();

        // Route through the real /legacy group (LegacyApiKeyFilter + MapLegacyEndpoints), not
        // MapNewBowler() directly, so this test actually exercises the filter that protects the
        // route as deployed, and the relative path registered in NewBowler.cs.
        _app.MapLegacyGroup();

        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    [Fact(DisplayName = "POST /legacy/bowlers/new returns 401 and does not enqueue a job when the X-Api-Key header is missing")]
    public async Task Post_ShouldReturn401AndNotEnqueue_WhenApiKeyHeaderIsMissing()
    {
        // Arrange
        using var client = _app.GetTestClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/bowlers/new",
            new NewBowlerRequest(42),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /legacy/bowlers/new returns 401 and does not enqueue a job when the X-Api-Key header is wrong")]
    public async Task Post_ShouldReturn401AndNotEnqueue_WhenApiKeyHeaderIsWrong()
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/bowlers/new",
            new NewBowlerRequest(42),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /legacy/bowlers/new returns 400 and does not enqueue a job when BowlerId is invalid")]
    public async Task Post_ShouldReturn400AndNotEnqueue_WhenBowlerIdIsInvalid()
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ValidApiKey);

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/bowlers/new",
            new NewBowlerRequest(0),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /legacy/bowlers/new returns 202 and enqueues a NewBowlerSyncJob with the request's BowlerId")]
    public async Task Post_ShouldReturn202AndEnqueueSyncJob_WhenApiKeyAndBowlerIdAreValid()
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
            "/legacy/bowlers/new",
            new NewBowlerRequest(42),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        capturedJob.ShouldNotBeNull();
        capturedJob.Type.ShouldBe(typeof(NewBowlerSyncJob));
        capturedJob.Method.Name.ShouldBe(nameof(NewBowlerSyncJob.SyncAsync));
        capturedJob.Args[0].ShouldBe(42);
    }
}

[UnitTest]
[Component("Legacy")]
public sealed class LegacyBowlerExtensionsTests
{
    [Fact(DisplayName = "CreateFromLegacy should return a Bowler with the mapped fields when the name is valid")]
    public void CreateFromLegacy_ShouldReturnBowlerWithMappedFields_WhenNameIsValid()
    {
        // Act
        var result = Bowler.CreateFromLegacy(
            "David",
            "Smith",
            middleName: "M",
            suffix: NameSuffix.Jr,
            legacyId: 123,
            gender: Gender.Male,
            dateOfBirth: new DateOnly(1990, 1, 1));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Name.FirstName.ShouldBe("David");
        result.Value.Name.LastName.ShouldBe("Smith");
        result.Value.Name.MiddleName.ShouldBe("M");
        result.Value.Name.Suffix.ShouldBe(NameSuffix.Jr);
        result.Value.LegacyId.ShouldBe(123);
        result.Value.Gender.ShouldBe(Gender.Male);
        result.Value.DateOfBirth.ShouldBe(new DateOnly(1990, 1, 1));
    }

    [Fact(DisplayName = "CreateFromLegacy should return an error when the first name is blank")]
    public void CreateFromLegacy_ShouldReturnError_WhenFirstNameIsBlank()
    {
        // Act
        var result = Bowler.CreateFromLegacy(string.Empty, "Smith");

        // Assert
        result.IsError.ShouldBeTrue();
    }
}

[IntegrationTest]
[Component("Legacy")]
[Collection<AppDbContextFixture>]
public sealed class NewBowlerSyncJobTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private NpgsqlConnection _legacyConnection = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();

        // Plain Dapper works against any real ADO.NET IDbConnection, so a Postgres connection
        // (reusing this test project's existing Testcontainers.PostgreSql infra) stands in for the
        // real MSSQL neba-fwk database here rather than standing up a second, MSSQL-specific
        // container for one temporary backdoor query. A CREATE TEMP TABLE is scoped to this single
        // connection - it's gone as soon as the connection closes, so it needs no cleanup and can't
        // collide with the shared fixture's own schema/Respawn reset.
        // SQLite was tried first and rejected: its dynamic typing returns Int64/string for every
        // INTEGER/TEXT column regardless of declared type, and Dapper's record-constructor mapping
        // requires the reader's column types to exactly match LegacyBowlerRow's (int, DateTime?,
        // ...) constructor parameters - Postgres's strict typing (int4 -> Int32, timestamp ->
        // DateTime) actually satisfies that, matching what Microsoft.Data.SqlClient would return
        // against the real MSSQL schema.
        _legacyConnection = new NpgsqlConnection(fixture.ConnectionString);
        await _legacyConnection.OpenAsync();

        // Unquoted identifiers here so Postgres folds them to lowercase, matching how it resolves
        // the production query's own unquoted column/table references (SELECT Id, ... FROM Bowlers).
        await using var create = _legacyConnection.CreateCommand();
        create.CommandText = """
            CREATE TEMP TABLE Bowlers (
                Id integer PRIMARY KEY,
                FirstName text NOT NULL,
                MiddleInitial text NULL,
                LastName text NOT NULL,
                Suffix text NULL,
                Gender integer NOT NULL,
                DateOfBirth timestamp NULL
            )
            """;
        await create.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _legacyConnection.DisposeAsync();
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task InsertLegacyBowlerAsync(
        int id,
        string firstName = "David",
        string? middleInitial = null,
        string lastName = "Smith",
        string? suffix = null,
        int gender = 0,
        DateTime? dateOfBirth = null)
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = """
            INSERT INTO Bowlers (Id, FirstName, MiddleInitial, LastName, Suffix, Gender, DateOfBirth)
            VALUES (@Id, @FirstName, @MiddleInitial, @LastName, @Suffix, @Gender, @DateOfBirth)
            """;
        insert.Parameters.AddWithValue("@Id", id);
        insert.Parameters.AddWithValue("@FirstName", firstName);
        insert.Parameters.AddWithValue("@MiddleInitial", (object?)middleInitial ?? DBNull.Value);
        insert.Parameters.AddWithValue("@LastName", lastName);
        insert.Parameters.AddWithValue("@Suffix", (object?)suffix ?? DBNull.Value);
        insert.Parameters.AddWithValue("@Gender", gender);
        insert.Parameters.AddWithValue("@DateOfBirth", dateOfBirth is null ? DBNull.Value : dateOfBirth.Value);
        await insert.ExecuteNonQueryAsync();
    }

    private NewBowlerSyncJob CreateJob(FakeLogger<NewBowlerSyncJob>? logger = null) =>
        new(_dbContext, _legacyConnection, logger ?? new FakeLogger<NewBowlerSyncJob>());

    [Fact(DisplayName = "SyncAsync should not create a bowler and should log a warning when the legacy id is not found")]
    public async Task SyncAsync_ShouldNotCreateBowlerAndShouldLogWarning_WhenLegacyIdIsNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fakeLogger = new FakeLogger<NewBowlerSyncJob>();
        var job = CreateJob(fakeLogger);

        // Act
        await job.SyncAsync(999, ct);

        // Assert
        (await _dbContext.Set<Bowler>().AnyAsync(ct)).ShouldBeFalse();
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("999");
    }

    [Fact(DisplayName = "SyncAsync should persist a bowler mapped from the legacy row when the legacy id is found")]
    public async Task SyncAsync_ShouldPersistMappedBowler_WhenLegacyIdIsFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(
            1,
            firstName: "David",
            middleInitial: "M",
            lastName: "Smith",
            suffix: "Jr",
            gender: 0,
            dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().SingleAsync(b => b.LegacyId == 1, ct);
        bowler.Name.FirstName.ShouldBe("David");
        bowler.Name.MiddleName.ShouldBe("M");
        bowler.Name.LastName.ShouldBe("Smith");
        bowler.Name.Suffix.ShouldBe(NameSuffix.Jr);
        bowler.Gender.ShouldBe(Gender.Male);
        bowler.DateOfBirth.ShouldBe(new DateOnly(1990, 1, 1));
    }

    [Theory(DisplayName = "SyncAsync should map the legacy Gender column to the right Gender")]
    [InlineData(0, "Male")]
    [InlineData(1, "Female")]
    [InlineData(-1, null)]
    [InlineData(2, null)]
    public async Task SyncAsync_ShouldMapGender_FromLegacyGenderColumn(int legacyGender, string? expectedGenderName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(1, gender: legacyGender);
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().SingleAsync(b => b.LegacyId == 1, ct);
        bowler.Gender?.Name.ShouldBe(expectedGenderName);
    }

    [Fact(DisplayName = "SyncAsync should leave DateOfBirth null when the legacy row has no date of birth")]
    public async Task SyncAsync_ShouldLeaveDateOfBirthNull_WhenLegacyRowHasNoDateOfBirth()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(1, dateOfBirth: null);
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().SingleAsync(b => b.LegacyId == 1, ct);
        bowler.DateOfBirth.ShouldBeNull();
    }

    [Theory(DisplayName = "SyncAsync should map a recognized legacy suffix regardless of trailing period or case")]
    [InlineData("Jr", "Jr")]
    [InlineData("Jr.", "Jr")]
    [InlineData("JR.", "Jr")]
    [InlineData("II", "II")]
    public async Task SyncAsync_ShouldMapRecognizedSuffix_RegardlessOfTrailingPeriodOrCase(string legacySuffix, string expectedSuffixName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(1, suffix: legacySuffix);
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().SingleAsync(b => b.LegacyId == 1, ct);
        bowler.Name.Suffix.ShouldNotBeNull();
        bowler.Name.Suffix.Name.ShouldBe(expectedSuffixName);
    }

    [Theory(DisplayName = "SyncAsync should leave the suffix null when the legacy suffix is blank")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SyncAsync_ShouldLeaveSuffixNull_WhenLegacySuffixIsBlank(string? legacySuffix)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(1, suffix: legacySuffix);
        var fakeLogger = new FakeLogger<NewBowlerSyncJob>();
        var job = CreateJob(fakeLogger);

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().SingleAsync(b => b.LegacyId == 1, ct);
        bowler.Name.Suffix.ShouldBeNull();
        fakeLogger.Collector.GetSnapshot().ShouldBeEmpty();
    }

    [Fact(DisplayName = "SyncAsync should leave the suffix null and log a warning when the legacy suffix is unrecognized")]
    public async Task SyncAsync_ShouldLeaveSuffixNullAndLogWarning_WhenLegacySuffixIsUnrecognized()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(1, suffix: "Esq");
        var fakeLogger = new FakeLogger<NewBowlerSyncJob>();
        var job = CreateJob(fakeLogger);

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().SingleAsync(b => b.LegacyId == 1, ct);
        bowler.Name.Suffix.ShouldBeNull();
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("Esq");
    }

    [Fact(DisplayName = "SyncAsync should not create a bowler and should log an error when the mapped name is invalid")]
    public async Task SyncAsync_ShouldNotCreateBowlerAndShouldLogError_WhenMappedNameIsInvalid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(1, firstName: string.Empty);
        var fakeLogger = new FakeLogger<NewBowlerSyncJob>();
        var job = CreateJob(fakeLogger);

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        (await _dbContext.Set<Bowler>().AnyAsync(ct)).ShouldBeFalse();
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Error);
    }

    [Fact(DisplayName = "SyncAsync should be a no-op and should log information when the legacy id was already synced")]
    public async Task SyncAsync_ShouldBeNoOpAndLogInformation_WhenLegacyIdWasAlreadySynced()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var existing = BowlerFactory.Create(legacyId: 1, name: NameFactory.Create(firstName: "Original", lastName: "Bowler"));
        await _dbContext.Set<Bowler>().AddAsync(existing, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        await InsertLegacyBowlerAsync(1, firstName: "Changed", lastName: "Name");
        var fakeLogger = new FakeLogger<NewBowlerSyncJob>();
        var job = CreateJob(fakeLogger);

        // Act
        await job.SyncAsync(1, ct);

        // Assert - strictly create-only: a repeat call for the same LegacyId is a no-op, never an update.
        var bowlers = await _dbContext.Set<Bowler>().Where(b => b.LegacyId == 1).ToListAsync(ct);
        bowlers.ShouldHaveSingleItem();
        bowlers[0].Name.FirstName.ShouldBe("Original");
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Information);
    }
}