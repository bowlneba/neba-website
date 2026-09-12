using System.Diagnostics.CodeAnalysis;

using Audit.AzureStorageTables.ConfigurationApi;
using Audit.Core;
using Audit.EntityFramework;

using Azure.Data.Tables;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Database;
using Neba.Api.Database.Interceptors;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Legacy.Bowlers;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;

using Shouldly;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Auditing;

// Regression coverage for UpdateBowler.SyncAsync's `db.Entry(existing).State = EntityState.Modified`
// fix. Exercises the real production job (not a hand-rolled reproduction of its EF mechanics)
// against the real Azure Table Storage audit path (via Azurite, mirroring
// AuditingConfiguration.AddAuditing()'s Bowler/Name opt-in exactly), since the actual defect only
// shows up in the persisted audit event's content: without a Bowler entry present, a name-only
// change's audit trail is an add/delete pair for the owned Name value with nothing to tie it back
// to a specific bowler.
//
// Not part of UpdateBowlerSyncJobTests/LegacyDatabasesTestScope: that collection runs concurrently
// with other test collections, and Audit.Core/Audit.EntityFramework's configuration is process-wide
// static state - mutating it outside the "AuditConfigurationSequential" collection would race
// against every other audit-config test in the suite (see CLAUDE.md's "Process-Wide Static State
// Leaks" learning). This class pays for its own LegacySqlServerFixture instance instead.
[IntegrationTest]
[Component("Auditing")]
[Collection("AuditConfigurationSequential")]
public sealed class BowlerNameChangeAuditIntegrationTests(
    AppDbContextFixture appDbContextFixture,
    LegacySqlServerFixture legacyFixture,
    AzuriteFixture azuriteFixture)
    : IClassFixture<AppDbContextFixture>, IClassFixture<LegacySqlServerFixture>, IClassFixture<AzuriteFixture>, IAsyncLifetime
{
    private const string TableName = "BowlerNameChangeAuditEvents";

    private readonly PooledAuditSaveChangesInterceptor _auditInterceptor = new();

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed",
        Justification = "Ownership stays with LegacySqlServerFixture, which disposes it once for the whole run.")]
    private LegacySqlServerDatabase _legacyDatabase = null!;
    private ServiceProvider _serviceProvider = null!;
    private TableClient _tableClient = null!;

    private SqlConnection _legacyConnection => _legacyDatabase.Connection;

    public async ValueTask InitializeAsync()
    {
        await appDbContextFixture.ResetAsync();

        var services = new ServiceCollection();
        services.AddFusionCache().WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromHours(1));
        _serviceProvider = services.BuildServiceProvider();

        _legacyDatabase = await legacyFixture.GetOrCreateDatabaseAsync(nameof(BowlerNameChangeAuditIntegrationTests), CreateSchemaAsync);
        await _legacyDatabase.ResetAsync();

        _tableClient = new TableClient(azuriteFixture.ConnectionString, TableName);
        await _tableClient.CreateIfNotExistsAsync(TestContext.Current.CancellationToken);
        await ClearTableAsync();

        Audit.Core.Configuration.Setup()
            .UseAzureTableStorage(config => config
                .ConnectionString(azuriteFixture.ConnectionString)
                .TableName(_ => TableName)
                // EntityMapper (not EntityBuilder) retains the event payload as a JSON column -
                // see AuditingConfiguration.AddAuditing()'s identical setup for why.
                .EntityMapper(ev => new AuditEventTableEntity(ev.EventType ?? "unknown", Ulid.NewUlid().ToString(), ev)))
            .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

        // Mirrors AuditingConfiguration.AddAuditing()'s real Bowler/Name opt-in exactly - see that
        // file's comment on why Name needs its own Include<T>() despite table-splitting into Bowler.
        Audit.EntityFramework.Configuration.Setup()
            .ForContext<AppDbContext>(auditConfig => auditConfig
                .AuditEventType("EF:{context}")
                .IncludeEntityObjects(true))
            .UseOptIn()
            .Include<Bowler>()
            .Include<Name>();
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await appDbContextFixture.ResetAsync();
    }

    private async Task ClearTableAsync()
    {
        await foreach (var entity in _tableClient.QueryAsync<TableEntity>(cancellationToken: TestContext.Current.CancellationToken))
        {
            await _tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, cancellationToken: TestContext.Current.CancellationToken);
        }
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
            )
            """;
        await create.ExecuteNonQueryAsync();
    }

    [Fact(DisplayName = "SyncAsync should audit the full Bowler entry alongside the Name change when only the name changes")]
    public async Task SyncAsync_ShouldAuditFullBowlerEntry_WhenOnlyNameChanges()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        var bowler = BowlerFactory.Create(
            name: NameFactory.Create(firstName: "David", lastName: "Smith"),
            legacyId: 1,
            gender: Gender.Male,
            dateOfBirth: null);

        await using (var seedContext = appDbContextFixture.CreateDbContext(_auditInterceptor))
        {
            await seedContext.Set<Bowler>().AddAsync(bowler, ct);
            await seedContext.SaveChangesAsync(ct);
        }

        // Only the name is changing (a nickname is being added) - Gender and DateOfBirth in the
        // legacy row map to the same values already on the bowler, matching the exact "just a name
        // update" scenario that dropped audit coverage before the fix.
        await using var insertLegacy = _legacyConnection.CreateCommand();
        insertLegacy.CommandText = """
            INSERT INTO Bowlers (Id, FirstName, MiddleInitial, LastName, Suffix, Gender, DateOfBirth)
            VALUES (@Id, @FirstName, @MiddleInitial, @LastName, @Suffix, @Gender, @DateOfBirth)
            """;
        insertLegacy.Parameters.AddWithValue("@Id", 1);
        insertLegacy.Parameters.AddWithValue("@FirstName", "David \"Bulldog\"");
        insertLegacy.Parameters.AddWithValue("@MiddleInitial", DBNull.Value);
        insertLegacy.Parameters.AddWithValue("@LastName", "Smith");
        insertLegacy.Parameters.AddWithValue("@Suffix", DBNull.Value);
        insertLegacy.Parameters.AddWithValue("@Gender", 0);
        insertLegacy.Parameters.AddWithValue("@DateOfBirth", DBNull.Value);
        await insertLegacy.ExecuteNonQueryAsync(ct);

        await using var dbContext = appDbContextFixture.CreateDbContext(_auditInterceptor);
        var job = new UpdateBowlerSyncJob(
            dbContext,
            _legacyConnection,
            _serviceProvider.GetRequiredService<IFusionCache>(),
            new FakeLogger<UpdateBowlerSyncJob>());

        // Act
        await job.SyncAsync(1, "test-correlation-id", ct);

        // Assert
        var updateEvent = await FindEventWithBowlerUpdateAsync();

        var bowlerEntry = updateEvent.Entries.Single(e => e.Name == nameof(Bowler));
        bowlerEntry.Action.ShouldBe("Update");
        bowlerEntry.ColumnValues.ShouldNotBeNull();
        bowlerEntry.ColumnValues["legacy_id"].ToString().ShouldBe("1");

        // The owned Name change still shows up as an add/delete pair - that's fine, as long as the
        // Bowler entry above is in the same event to tie it back to a specific bowler.
        updateEvent.Entries.ShouldContain(e => e.Name == nameof(Name) && e.Action == "Insert");
        updateEvent.Entries.ShouldContain(e => e.Name == nameof(Name) && e.Action == "Delete");
    }

    private async Task<EntityFrameworkEvent> FindEventWithBowlerUpdateAsync()
    {
        await foreach (var entity in _tableClient.QueryAsync<TableEntity>(cancellationToken: TestContext.Current.CancellationToken))
        {
            var json = entity.GetString("AuditEvent");
            if (json is null)
            {
                continue;
            }

            var auditEvent = Audit.Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(json);
            var efEvent = auditEvent.GetEntityFrameworkEvent();

            if (efEvent?.Entries.Any(e => e.Name == nameof(Bowler) && e.Action == "Update") == true)
            {
                return efEvent;
            }
        }

        throw new InvalidOperationException("No audit event containing a Bowler Update entry was found.");
    }
}