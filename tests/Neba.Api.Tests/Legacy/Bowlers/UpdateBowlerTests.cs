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

// Mirrors the single-file shape of the production source (Legacy/Bowlers/UpdateBowler.cs) so the
// whole test suite for this backdoor action is removed alongside it at sunset with no leftover
// test files to hunt down.

[UnitTest]
[Component("Legacy")]
public sealed class BowlerApplyLegacyUpdateTests
{
    [Fact(DisplayName = "ApplyLegacyUpdate should update Name, Gender and DateOfBirth when the name is valid")]
    public void ApplyLegacyUpdate_ShouldUpdateNameGenderAndDateOfBirth_WhenNameIsValid()
    {
        // Arrange
        var bowler = BowlerFactory.Create(
            name: NameFactory.Create(firstName: "Original", lastName: "Bowler"),
            gender: Gender.Male,
            dateOfBirth: new DateOnly(1980, 1, 1));

        // Act
        var result = bowler.ApplyLegacyUpdate(
            "David",
            "Smith",
            middleName: "M",
            suffix: NameSuffix.Jr,
            nickname: "Dave",
            gender: Gender.Female,
            dateOfBirth: new DateOnly(1990, 6, 15));

        // Assert
        result.IsError.ShouldBeFalse();
        bowler.Name.FirstName.ShouldBe("David");
        bowler.Name.LastName.ShouldBe("Smith");
        bowler.Name.MiddleName.ShouldBe("M");
        bowler.Name.Suffix.ShouldBe(NameSuffix.Jr);
        bowler.Name.Nickname.ShouldBe("Dave");
        bowler.Gender.ShouldBe(Gender.Female);
        bowler.DateOfBirth.ShouldBe(new DateOnly(1990, 6, 15));
    }

    [Fact(DisplayName = "ApplyLegacyUpdate should set Gender and DateOfBirth to null when not provided")]
    public void ApplyLegacyUpdate_ShouldSetGenderAndDateOfBirthToNull_WhenNotProvided()
    {
        // Arrange
        var bowler = BowlerFactory.Create(gender: Gender.Male, dateOfBirth: new DateOnly(1980, 1, 1));

        // Act
        var result = bowler.ApplyLegacyUpdate("David", "Smith");

        // Assert
        result.IsError.ShouldBeFalse();
        bowler.Gender.ShouldBeNull();
        bowler.DateOfBirth.ShouldBeNull();
    }

    [Fact(DisplayName = "ApplyLegacyUpdate should return an error and leave the bowler unchanged when the first name is blank")]
    public void ApplyLegacyUpdate_ShouldReturnErrorAndLeaveBowlerUnchanged_WhenFirstNameIsBlank()
    {
        // Arrange
        var originalName = NameFactory.Create(firstName: "Original", lastName: "Bowler");
        var bowler = BowlerFactory.Create(name: originalName, gender: Gender.Male, dateOfBirth: new DateOnly(1980, 1, 1));

        // Act
        var result = bowler.ApplyLegacyUpdate(string.Empty, "Smith");

        // Assert
        result.IsError.ShouldBeTrue();
        bowler.Name.ShouldBe(originalName);
        bowler.Gender.ShouldBe(Gender.Male);
        bowler.DateOfBirth.ShouldBe(new DateOnly(1980, 1, 1));
    }
}

[UnitTest]
[Component("Legacy")]
public sealed class UpdateBowlerRequestValidatorTests
{
    private readonly UpdateBowlerRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when BowlerId is greater than zero")]
    public void Validate_ShouldSucceed_WhenBowlerIdIsGreaterThanZero()
    {
        // Arrange
        var request = new UpdateBowlerRequest(1);

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
        var request = new UpdateBowlerRequest(bowlerId);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateBowlerRequest.BowlerId));
    }
}

[IntegrationTest]
[Component("Legacy")]
public sealed class UpdateBowlerEndpointTests : IAsyncLifetime
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
        builder.Services.AddScoped<IValidator<UpdateBowlerRequest>, UpdateBowlerRequestValidator>();
        builder.Services.AddSingleton(Options.Create(new LegacySettings { ApiKey = ValidApiKey }));

        _app = builder.Build();

        // Route through the real /legacy group (LegacyApiKeyFilter + MapLegacyEndpoints), not
        // MapUpdateBowler() directly, so this test actually exercises the filter that protects the
        // route as deployed, and the relative path registered in UpdateBowler.cs.
        _app.MapLegacyGroup();

        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    [Fact(DisplayName = "POST /legacy/bowlers/update returns 401 and does not enqueue a job when the X-Api-Key header is missing")]
    public async Task Post_ShouldReturn401AndNotEnqueue_WhenApiKeyHeaderIsMissing()
    {
        // Arrange
        using var client = _app.GetTestClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/bowlers/update",
            new UpdateBowlerRequest(42),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /legacy/bowlers/update returns 401 and does not enqueue a job when the X-Api-Key header is wrong")]
    public async Task Post_ShouldReturn401AndNotEnqueue_WhenApiKeyHeaderIsWrong()
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/bowlers/update",
            new UpdateBowlerRequest(42),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /legacy/bowlers/update returns 400 and does not enqueue a job when BowlerId is invalid")]
    public async Task Post_ShouldReturn400AndNotEnqueue_WhenBowlerIdIsInvalid()
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ValidApiKey);

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/bowlers/update",
            new UpdateBowlerRequest(0),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /legacy/bowlers/update returns 202 and enqueues an UpdateBowlerSyncJob with the request's BowlerId")]
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
            "/legacy/bowlers/update",
            new UpdateBowlerRequest(42),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        capturedJob.ShouldNotBeNull();
        capturedJob.Type.ShouldBe(typeof(UpdateBowlerSyncJob));
        capturedJob.Method.Name.ShouldBe(nameof(UpdateBowlerSyncJob.SyncAsync));
        capturedJob.Args[0].ShouldBe(42);
    }
}

[IntegrationTest]
[Component("Legacy")]
[Collection<AppDbContextFixture>]
public sealed class UpdateBowlerSyncJobTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private NpgsqlConnection _legacyConnection = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();

        // See NewBowlerSyncJobTests for the full rationale on standing in a Postgres connection for
        // the real MSSQL neba-fwk database via a connection-scoped CREATE TEMP TABLE.
        _legacyConnection = new NpgsqlConnection(fixture.ConnectionString);
        await _legacyConnection.OpenAsync();

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

    private async Task<Bowler> SeedExistingBowlerAsync(int legacyId, Name? name = null, Gender? gender = null, DateOnly? dateOfBirth = null)
    {
        var bowler = BowlerFactory.Create(name: name, legacyId: legacyId, gender: gender, dateOfBirth: dateOfBirth);
        await _dbContext.Set<Bowler>().AddAsync(bowler, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        _dbContext.ChangeTracker.Clear();
        return bowler;
    }

    private UpdateBowlerSyncJob CreateJob(FakeLogger<UpdateBowlerSyncJob>? logger = null) =>
        new(_dbContext, _legacyConnection, logger ?? new FakeLogger<UpdateBowlerSyncJob>());

    [Fact(DisplayName = "SyncAsync should not persist anything and should log a warning when the legacy id is not found")]
    public async Task SyncAsync_ShouldNotPersistAnythingAndShouldLogWarning_WhenLegacyIdIsNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fakeLogger = new FakeLogger<UpdateBowlerSyncJob>();
        var job = CreateJob(fakeLogger);

        // Act
        await job.SyncAsync(999, ct);

        // Assert
        (await _dbContext.Set<Bowler>().AnyAsync(ct)).ShouldBeFalse();
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("999");
    }

    [Fact(DisplayName = "SyncAsync should update the existing bowler's mapped fields when the legacy id already has a website record")]
    public async Task SyncAsync_ShouldUpdateExistingBowlersMappedFields_WhenLegacyIdAlreadyHasWebsiteRecord()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedExistingBowlerAsync(
            1,
            name: NameFactory.Create(firstName: "Original", lastName: "Bowler"),
            gender: Gender.Male,
            dateOfBirth: new DateOnly(1980, 1, 1));

        await InsertLegacyBowlerAsync(
            1,
            firstName: "David",
            middleInitial: "M",
            lastName: "Smith",
            suffix: "Jr",
            gender: 1,
            dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var bowlers = await _dbContext.Set<Bowler>().Where(b => b.LegacyId == 1).ToListAsync(ct);
        bowlers.ShouldHaveSingleItem();
        var bowler = bowlers[0];
        bowler.Name.FirstName.ShouldBe("David");
        bowler.Name.MiddleName.ShouldBe("M");
        bowler.Name.LastName.ShouldBe("Smith");
        bowler.Name.Suffix.ShouldBe(NameSuffix.Jr);
        bowler.Gender.ShouldBe(Gender.Female);
        bowler.DateOfBirth.ShouldBe(new DateOnly(1990, 1, 1));
    }

    [Fact(DisplayName = "SyncAsync should extract a quoted nickname from the legacy first name when updating an existing bowler")]
    public async Task SyncAsync_ShouldExtractQuotedNickname_WhenUpdatingExistingBowler()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedExistingBowlerAsync(1);
        await InsertLegacyBowlerAsync(1, firstName: "William \"Bill\"");
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().SingleAsync(b => b.LegacyId == 1, ct);
        bowler.Name.FirstName.ShouldBe("William");
        bowler.Name.Nickname.ShouldBe("Bill");
    }

    [Theory(DisplayName = "SyncAsync should map the legacy Gender column to the right Gender when updating an existing bowler")]
    [InlineData(0, "Male")]
    [InlineData(1, "Female")]
    [InlineData(-1, null)]
    [InlineData(2, null)]
    public async Task SyncAsync_ShouldMapGender_FromLegacyGenderColumn_WhenUpdatingExistingBowler(int legacyGender, string? expectedGenderName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedExistingBowlerAsync(1, gender: Gender.Male, dateOfBirth: new DateOnly(1980, 1, 1));
        await InsertLegacyBowlerAsync(1, gender: legacyGender);
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().SingleAsync(b => b.LegacyId == 1, ct);
        bowler.Gender?.Name.ShouldBe(expectedGenderName);
    }

    [Fact(DisplayName = "SyncAsync should set DateOfBirth to null when the legacy row has no date of birth")]
    public async Task SyncAsync_ShouldSetDateOfBirthToNull_WhenLegacyRowHasNoDateOfBirth()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedExistingBowlerAsync(1, dateOfBirth: new DateOnly(1980, 1, 1));
        await InsertLegacyBowlerAsync(1, dateOfBirth: null);
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().SingleAsync(b => b.LegacyId == 1, ct);
        bowler.DateOfBirth.ShouldBeNull();
    }

    [Theory(DisplayName = "SyncAsync should map a recognized legacy suffix regardless of trailing period or case when updating an existing bowler")]
    [InlineData("Jr", "Jr")]
    [InlineData("Jr.", "Jr")]
    [InlineData("JR.", "Jr")]
    [InlineData("II", "II")]
    public async Task SyncAsync_ShouldMapRecognizedSuffix_RegardlessOfTrailingPeriodOrCase_WhenUpdatingExistingBowler(string legacySuffix, string expectedSuffixName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedExistingBowlerAsync(1);
        await InsertLegacyBowlerAsync(1, suffix: legacySuffix);
        var job = CreateJob();

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().SingleAsync(b => b.LegacyId == 1, ct);
        bowler.Name.Suffix.ShouldNotBeNull();
        bowler.Name.Suffix.Name.ShouldBe(expectedSuffixName);
    }

    [Fact(DisplayName = "SyncAsync should leave the suffix null and log a warning when the legacy suffix is unrecognized during an update")]
    public async Task SyncAsync_ShouldLeaveSuffixNullAndLogWarning_WhenLegacySuffixIsUnrecognizedDuringUpdate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedExistingBowlerAsync(1);
        await InsertLegacyBowlerAsync(1, suffix: "Esq");
        var fakeLogger = new FakeLogger<UpdateBowlerSyncJob>();
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

    [Fact(DisplayName = "SyncAsync should not persist the update and should log an error when the mapped name is invalid")]
    public async Task SyncAsync_ShouldNotPersistUpdateAndShouldLogError_WhenMappedNameIsInvalid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedExistingBowlerAsync(1, name: NameFactory.Create(firstName: "Original", lastName: "Bowler"));
        await InsertLegacyBowlerAsync(1, firstName: string.Empty);
        var fakeLogger = new FakeLogger<UpdateBowlerSyncJob>();
        var job = CreateJob(fakeLogger);

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().SingleAsync(b => b.LegacyId == 1, ct);
        bowler.Name.FirstName.ShouldBe("Original");
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Error);
    }

    [Fact(DisplayName = "SyncAsync should create a bowler and log information when the legacy id has no existing website record")]
    public async Task SyncAsync_ShouldCreateBowlerAndLogInformation_WhenLegacyIdHasNoExistingWebsiteRecord()
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
        var fakeLogger = new FakeLogger<UpdateBowlerSyncJob>();
        var job = CreateJob(fakeLogger);

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
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Information);
    }

    [Fact(DisplayName = "SyncAsync should not create a bowler and should log an error when falling back to create with an invalid mapped name")]
    public async Task SyncAsync_ShouldNotCreateBowlerAndShouldLogError_WhenFallingBackToCreateWithInvalidMappedName()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(1, firstName: string.Empty);
        var fakeLogger = new FakeLogger<UpdateBowlerSyncJob>();
        var job = CreateJob(fakeLogger);

        // Act
        await job.SyncAsync(1, ct);

        // Assert
        (await _dbContext.Set<Bowler>().AnyAsync(ct)).ShouldBeFalse();
        var records = fakeLogger.Collector.GetSnapshot();
        records.Count.ShouldBe(2);
        records.ShouldContain(r => r.Level == LogLevel.Information);
        records.ShouldContain(r => r.Level == LogLevel.Error);
    }
}