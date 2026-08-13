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
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Legacy;
using Neba.Api.Legacy.Bowlers;
using Neba.Api.Legacy.HallOfFame;
using Neba.Api.Legacy.Tournaments;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.HallOfFame;
using Neba.TestFactory.Infrastructure;

using Npgsql;

namespace Neba.Api.Tests.Legacy.HallOfFame;

// Mirrors the single-file shape of the production source (Legacy/HallOfFame/HallOfFameTests.cs) so the
// whole test suite for this backdoor action is removed alongside it at sunset with no leftover
// test files to hunt down.

[UnitTest]
[Component("Legacy")]
public sealed class NewHallOfFameInductionRequestValidatorTests
{
    private readonly NewHallOfFameInductionRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when HallOfFameIds is non-empty and every id is greater than zero")]
    public void Validate_ShouldSucceed_WhenHallOfFameIdsIsNonEmptyAndEveryIdIsGreaterThanZero()
    {
        // Arrange
        var request = new NewHallOfFameInductionRequest([1, 2, 3]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail when HallOfFameIds is empty")]
    public void Validate_ShouldFail_WhenHallOfFameIdsIsEmpty()
    {
        // Arrange
        var request = new NewHallOfFameInductionRequest([]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NewHallOfFameInductionRequest.HallOfFameIds));
    }

    [Theory(DisplayName = "Validate should fail when any HallOfFameIds entry is not greater than zero")]
    [InlineData(0, TestDisplayName = "Zero")]
    [InlineData(-1, TestDisplayName = "Negative")]
    public void Validate_ShouldFail_WhenAnyHallOfFameIdsEntryIsNotGreaterThanZero(int invalidId)
    {
        // Arrange
        var request = new NewHallOfFameInductionRequest([1, invalidId]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
    }
}

[IntegrationTest]
[Component("Legacy")]
public sealed class NewHallOfFameInductionEndpointTests : IAsyncLifetime
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
        builder.Services.AddScoped<IValidator<NewHallOfFameInductionRequest>, NewHallOfFameInductionRequestValidator>();
        // NewBowlerRequest's/UpdateBowlerRequest's/NewTournamentRequest's validators are also
        // required here: MapLegacyGroup() below maps every endpoint in the /legacy group (not just
        // this one), and ASP.NET Core builds route metadata for the whole group on the first request
        // to any of its endpoints - an unregistered IValidator<T> for a sibling endpoint throws at
        // that point, not just when that sibling is called.
        builder.Services.AddScoped<IValidator<NewBowlerRequest>, NewBowlerRequestValidator>();
        builder.Services.AddScoped<IValidator<UpdateBowlerRequest>, UpdateBowlerRequestValidator>();
        builder.Services.AddScoped<IValidator<NewTournamentRequest>, NewTournamentRequestValidator>();
        builder.Services.AddSingleton(Options.Create(new LegacySettings { ApiKey = ValidApiKey }));

        _app = builder.Build();

        // Route through the real /legacy group (LegacyApiKeyFilter + MapLegacyEndpoints), not
        // MapNewHallOfFameInduction() directly, so this test actually exercises the filter that
        // protects the route as deployed, and the relative path registered in HallOfFameTests.cs.
        _app.MapLegacyGroup();

        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    [Fact(DisplayName = "POST /legacy/hall-of-fame/new returns 401 and does not enqueue a job when the X-Api-Key header is missing")]
    public async Task Post_ShouldReturn401AndNotEnqueue_WhenApiKeyHeaderIsMissing()
    {
        // Arrange
        using var client = _app.GetTestClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/hall-of-fame/new",
            new NewHallOfFameInductionRequest([42]),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /legacy/hall-of-fame/new returns 401 and does not enqueue a job when the X-Api-Key header is wrong")]
    public async Task Post_ShouldReturn401AndNotEnqueue_WhenApiKeyHeaderIsWrong()
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/hall-of-fame/new",
            new NewHallOfFameInductionRequest([42]),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /legacy/hall-of-fame/new returns 400 and does not enqueue a job when HallOfFameIds is empty")]
    public async Task Post_ShouldReturn400AndNotEnqueue_WhenHallOfFameIdsIsEmpty()
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ValidApiKey);

        // Act
        using var response = await client.PostAsJsonAsync(
            "/legacy/hall-of-fame/new",
            new NewHallOfFameInductionRequest([]),
            TestContext.Current.CancellationToken);

        // Assert - Strict mock: any Create call without a setup would throw, proving no job was enqueued.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /legacy/hall-of-fame/new returns 202 and enqueues a NewHallOfFameInductionSyncJob with the request's HallOfFameIds")]
    public async Task Post_ShouldReturn202AndEnqueueSyncJob_WhenApiKeyAndHallOfFameIdsAreValid()
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
            "/legacy/hall-of-fame/new",
            new NewHallOfFameInductionRequest([101, 102]),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        capturedJob.ShouldNotBeNull();
        capturedJob.Type.ShouldBe(typeof(NewHallOfFameInductionSyncJob));
        capturedJob.Method.Name.ShouldBe(nameof(NewHallOfFameInductionSyncJob.SyncAsync));
        capturedJob.Args[0].ShouldBe((IReadOnlyCollection<int>)[101, 102]);
    }
}

[UnitTest]
[Component("Legacy")]
public sealed class LegacyHallOfFameInductionExtensionsTests
{
    [Fact(DisplayName = "CreateFromLegacy should return an induction with the mapped fields and a single category")]
    public void CreateFromLegacy_ShouldReturnInductionWithMappedFieldsAndSingleCategory()
    {
        // Arrange
        var bowlerId = BowlerId.New();

        // Act
        var result = HallOfFameInduction.CreateFromLegacy(bowlerId, 2025, HallOfFameCategory.SuperiorPerformance);

        // Assert
        result.BowlerId.ShouldBe(bowlerId);
        result.Year.ShouldBe(2025);
        result.Categories.ShouldBe([HallOfFameCategory.SuperiorPerformance]);
    }

    [Fact(DisplayName = "ApplyLegacyCategory should add a category that is not already present")]
    public void ApplyLegacyCategory_ShouldAddCategory_WhenNotAlreadyPresent()
    {
        // Arrange
        var induction = HallOfFameInductionFactory.Create(categories: [HallOfFameCategory.SuperiorPerformance]);

        // Act
        induction.ApplyLegacyCategory(HallOfFameCategory.MeritoriousService);

        // Assert
        induction.Categories.ShouldBe([HallOfFameCategory.SuperiorPerformance, HallOfFameCategory.MeritoriousService], ignoreOrder: true);
    }

    [Fact(DisplayName = "ApplyLegacyCategory should be a no-op when the category is already present")]
    public void ApplyLegacyCategory_ShouldBeNoOp_WhenCategoryIsAlreadyPresent()
    {
        // Arrange
        var induction = HallOfFameInductionFactory.Create(categories: [HallOfFameCategory.SuperiorPerformance]);

        // Act
        induction.ApplyLegacyCategory(HallOfFameCategory.SuperiorPerformance);

        // Assert
        induction.Categories.ShouldBe([HallOfFameCategory.SuperiorPerformance]);
    }
}

[UnitTest]
[Component("Legacy")]
public sealed class HallOfFameBowlerNotFoundEmailTests
{
    [Fact(DisplayName = "ToHtmlBody should include the legacy Hall of Fame id, bowler id, and year")]
    public void ToHtmlBody_ShouldIncludeLegacyHallOfFameIdBowlerIdAndYear()
    {
        // Arrange
        var email = new HallOfFameBowlerNotFoundEmail(42, 7, 2025);

        // Act
        var body = email.ToHtmlBody();

        // Assert
        body.ShouldContain("42");
        body.ShouldContain("7");
        body.ShouldContain("2025");
    }
}

[IntegrationTest]
[Component("Legacy")]
[Collection<AppDbContextFixture>]
public sealed class NewHallOfFameInductionSyncJobTests(AppDbContextFixture fixture)
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
        _legacyConnection = new NpgsqlConnection(fixture.ConnectionString);
        await _legacyConnection.OpenAsync();

        // Unquoted identifiers here so Postgres folds them to lowercase, matching how it resolves
        // the production query's own unquoted column/table references.
        await using var create = _legacyConnection.CreateCommand();
        create.CommandText = """
            CREATE TEMP TABLE HallOfFame (
                Id integer PRIMARY KEY,
                BowlerId integer NOT NULL,
                Category integer NOT NULL,
                Year integer NOT NULL
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

    private async Task InsertLegacyHallOfFameRowAsync(int id, int bowlerId, int category, int year)
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = """
            INSERT INTO HallOfFame (Id, BowlerId, Category, Year)
            VALUES (@Id, @BowlerId, @Category, @Year)
            """;
        insert.Parameters.AddWithValue("@Id", id);
        insert.Parameters.AddWithValue("@BowlerId", bowlerId);
        insert.Parameters.AddWithValue("@Category", category);
        insert.Parameters.AddWithValue("@Year", year);
        await insert.ExecuteNonQueryAsync();
    }

    private async Task<Bowler> CreateSyncedBowlerAsync(int legacyBowlerId, CancellationToken ct)
    {
        var bowler = BowlerFactory.Create(legacyId: legacyBowlerId);
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await _dbContext.SaveChangesAsync(ct);
        return bowler;
    }

    private NewHallOfFameInductionSyncJob CreateJob(
        Mock<IEmailSender>? emailSender = null,
        FakeLogger<NewHallOfFameInductionSyncJob>? logger = null) =>
        new(
            _dbContext,
            _legacyConnection,
            (emailSender ?? new Mock<IEmailSender>(MockBehavior.Strict)).Object,
            logger ?? new FakeLogger<NewHallOfFameInductionSyncJob>());

    [Fact(DisplayName = "SyncAsync should log a warning and make no changes when a legacy id is not found")]
    public async Task SyncAsync_ShouldLogWarningAndMakeNoChanges_WhenLegacyIdIsNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fakeLogger = new FakeLogger<NewHallOfFameInductionSyncJob>();
        var job = CreateJob(logger: fakeLogger);

        // Act
        await job.SyncAsync([999], ct);

        // Assert - Strict email mock: any SendAsync call without a setup would throw.
        (await _dbContext.HallOfFameInductions.AnyAsync(ct)).ShouldBeFalse();
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("999");
    }

    [Fact(DisplayName = "SyncAsync should create an induction mapped from the legacy row when the bowler and category resolve")]
    public async Task SyncAsync_ShouldCreateMappedInduction_WhenBowlerAndCategoryResolve()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = await CreateSyncedBowlerAsync(legacyBowlerId: 5, ct);
        await InsertLegacyHallOfFameRowAsync(1, bowlerId: 5, category: 100, year: 2025);
        var job = CreateJob();

        // Act
        await job.SyncAsync([1], ct);

        // Assert
        var induction = await _dbContext.HallOfFameInductions.SingleAsync(i => i.BowlerId == bowler.Id, ct);
        induction.Year.ShouldBe(2025);
        induction.Categories.ShouldBe([HallOfFameCategory.SuperiorPerformance]);
    }

    [Theory(DisplayName = "SyncAsync should map the legacy Category column to the right website categories")]
    [InlineData(100, "Superior Performance")]
    [InlineData(200, "Meritorious Service")]
    [InlineData(500, "Friend of NEBA")]
    public async Task SyncAsync_ShouldMapCategory_FromLegacyCategoryColumn(int legacyCategory, string expectedCategoryName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = await CreateSyncedBowlerAsync(legacyBowlerId: 5, ct);
        await InsertLegacyHallOfFameRowAsync(1, bowlerId: 5, category: legacyCategory, year: 2025);
        var job = CreateJob();

        // Act
        await job.SyncAsync([1], ct);

        // Assert
        var induction = await _dbContext.HallOfFameInductions.SingleAsync(i => i.BowlerId == bowler.Id, ct);
        induction.Categories.ShouldHaveSingleItem().Name.ShouldBe(expectedCategoryName);
    }

    [Fact(DisplayName = "SyncAsync should map the legacy Combined category to both website categories on the same row")]
    public async Task SyncAsync_ShouldMapCombinedCategory_ToBothWebsiteCategories()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = await CreateSyncedBowlerAsync(legacyBowlerId: 5, ct);
        await InsertLegacyHallOfFameRowAsync(1, bowlerId: 5, category: 300, year: 2025);
        var job = CreateJob();

        // Act
        await job.SyncAsync([1], ct);

        // Assert
        var induction = await _dbContext.HallOfFameInductions.SingleAsync(i => i.BowlerId == bowler.Id, ct);
        induction.Categories.ShouldBe(
            [HallOfFameCategory.SuperiorPerformance, HallOfFameCategory.MeritoriousService],
            ignoreOrder: true);
    }

    [Fact(DisplayName = "SyncAsync should log a warning and create no induction when the legacy category is unmapped")]
    public async Task SyncAsync_ShouldLogWarningAndCreateNoInduction_WhenLegacyCategoryIsUnmapped()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await CreateSyncedBowlerAsync(legacyBowlerId: 5, ct);
        await InsertLegacyHallOfFameRowAsync(1, bowlerId: 5, category: 999, year: 2025);
        var fakeLogger = new FakeLogger<NewHallOfFameInductionSyncJob>();
        var job = CreateJob(logger: fakeLogger);

        // Act
        await job.SyncAsync([1], ct);

        // Assert
        (await _dbContext.HallOfFameInductions.AnyAsync(ct)).ShouldBeFalse();
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("999");
    }

    [Fact(DisplayName = "SyncAsync should send a manual-intervention email and create no induction when the bowler cannot be resolved")]
    public async Task SyncAsync_ShouldSendEmailAndCreateNoInduction_WhenBowlerCannotBeResolved()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyHallOfFameRowAsync(1, bowlerId: 999, category: 100, year: 2025);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        EmailMessage? sentMessage = null;
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => sentMessage = message)
            .Returns(Task.CompletedTask);

        var fakeLogger = new FakeLogger<NewHallOfFameInductionSyncJob>();
        var job = CreateJob(emailSender, fakeLogger);

        // Act
        await job.SyncAsync([1], ct);

        // Assert - Strict mock: the Setup above is the verification that SendAsync was called.
        (await _dbContext.HallOfFameInductions.AnyAsync(ct)).ShouldBeFalse();
        sentMessage.ShouldNotBeNull();
        sentMessage.To.ShouldBe("website@bowlneba.com");
        sentMessage.Subject.ShouldBe("Manual intervention needed: Hall of Fame induction");
        sentMessage.HtmlBody.ShouldContain("999");

        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
    }

    [Fact(DisplayName = "SyncAsync should merge two legacy rows for the same bowler and year into one induction when synced in one call")]
    public async Task SyncAsync_ShouldMergeTwoRowsIntoOneInduction_WhenSameBowlerAndYearSyncedInOneCall()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = await CreateSyncedBowlerAsync(legacyBowlerId: 5, ct);
        await InsertLegacyHallOfFameRowAsync(1, bowlerId: 5, category: 100, year: 2025);
        await InsertLegacyHallOfFameRowAsync(2, bowlerId: 5, category: 200, year: 2025);
        var job = CreateJob();

        // Act
        await job.SyncAsync([1, 2], ct);

        // Assert
        var inductions = await _dbContext.HallOfFameInductions.Where(i => i.BowlerId == bowler.Id).ToListAsync(ct);
        inductions.ShouldHaveSingleItem();
        inductions[0].Categories.ShouldBe(
            [HallOfFameCategory.SuperiorPerformance, HallOfFameCategory.MeritoriousService],
            ignoreOrder: true);
    }

    [Fact(DisplayName = "SyncAsync should merge a second legacy row into the existing induction when synced across two separate calls")]
    public async Task SyncAsync_ShouldMergeSecondRowIntoExistingInduction_WhenSyncedAcrossTwoSeparateCalls()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = await CreateSyncedBowlerAsync(legacyBowlerId: 5, ct);
        await InsertLegacyHallOfFameRowAsync(1, bowlerId: 5, category: 100, year: 2025);
        var firstJob = CreateJob();
        await firstJob.SyncAsync([1], ct);
        _dbContext.ChangeTracker.Clear();

        await InsertLegacyHallOfFameRowAsync(2, bowlerId: 5, category: 200, year: 2025);
        var secondJob = CreateJob();

        // Act
        await secondJob.SyncAsync([2], ct);

        // Assert
        var inductions = await _dbContext.HallOfFameInductions.Where(i => i.BowlerId == bowler.Id).ToListAsync(ct);
        inductions.ShouldHaveSingleItem();
        inductions[0].Categories.ShouldBe(
            [HallOfFameCategory.SuperiorPerformance, HallOfFameCategory.MeritoriousService],
            ignoreOrder: true);
    }

    [Fact(DisplayName = "SyncAsync should be a no-op and should log information when the legacy row's category was already synced")]
    public async Task SyncAsync_ShouldBeNoOpAndLogInformation_WhenLegacyRowCategoryWasAlreadySynced()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = await CreateSyncedBowlerAsync(legacyBowlerId: 5, ct);
        await InsertLegacyHallOfFameRowAsync(1, bowlerId: 5, category: 100, year: 2025);
        var firstJob = CreateJob();
        await firstJob.SyncAsync([1], ct);
        _dbContext.ChangeTracker.Clear();

        var fakeLogger = new FakeLogger<NewHallOfFameInductionSyncJob>();
        var secondJob = CreateJob(logger: fakeLogger);

        // Act
        await secondJob.SyncAsync([1], ct);

        // Assert - strictly merge-additive: a repeat call for the same legacy row's category is a no-op.
        var inductions = await _dbContext.HallOfFameInductions.Where(i => i.BowlerId == bowler.Id).ToListAsync(ct);
        inductions.ShouldHaveSingleItem();
        inductions[0].Categories.ShouldBe([HallOfFameCategory.SuperiorPerformance]);
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldContain("1");
    }

    [Fact(DisplayName = "SyncAsync should sync remaining rows when one row's bowler cannot be resolved")]
    public async Task SyncAsync_ShouldSyncRemainingRows_WhenOneRowBowlerCannotBeResolved()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = await CreateSyncedBowlerAsync(legacyBowlerId: 5, ct);
        await InsertLegacyHallOfFameRowAsync(1, bowlerId: 999, category: 100, year: 2025);
        await InsertLegacyHallOfFameRowAsync(2, bowlerId: 5, category: 200, year: 2025);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var job = CreateJob(emailSender);

        // Act
        await job.SyncAsync([1, 2], ct);

        // Assert - Strict mock: the Setup above is the verification that SendAsync was called.
        var induction = await _dbContext.HallOfFameInductions.SingleAsync(i => i.BowlerId == bowler.Id, ct);
        induction.Categories.ShouldBe([HallOfFameCategory.MeritoriousService]);
    }
}
