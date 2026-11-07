using Audit.Core;
using Audit.EntityFramework;

using Azure.Data.Tables;

using Neba.Api.Auditing;
using Neba.Api.Contracts.Memberships;
using Neba.Api.Database;
using Neba.Api.Database.Interceptors;
using Neba.Api.Features.Memberships.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;

using Shouldly;

namespace Neba.Api.Tests.Auditing;

// Applies the real opt-in list (AuditingConfiguration.ConfigureAppDbContextAuditing) rather than a copy,
// so removing BowlerMembership from it fails here. Audit.NET's configuration is process-wide static state,
// hence the sequential collection.
[IntegrationTest]
[Component("Auditing")]
[Collection("AuditConfigurationSequential")]
public sealed class BowlerMembershipAuditIntegrationTests(AppDbContextFixture appDbContextFixture, AzuriteFixture azuriteFixture)
    : IClassFixture<AppDbContextFixture>, IClassFixture<AzuriteFixture>, IAsyncLifetime
{
    private const string TableName = "BowlerMembershipAuditEvents";

    private readonly PooledAuditSaveChangesInterceptor _auditInterceptor = new();

    private TableClient _tableClient = null!;

    public async ValueTask InitializeAsync()
    {
        await appDbContextFixture.ResetAsync();

        _tableClient = new TableClient(azuriteFixture.ConnectionString, TableName);
        await _tableClient.CreateIfNotExistsAsync(TestContext.Current.CancellationToken);
        await ClearTableAsync();

        Audit.Core.Configuration.Setup()
            .UseAzureTableStorage(config => config
                .ConnectionString(azuriteFixture.ConnectionString)
                .TableName(_ => TableName)
                .EntityMapper(ev => ChunkedAuditEventTableEntity.Create(AuditingConfiguration.ToPartitionKey(ev.EventType), Ulid.NewUlid().ToString(), ev)))
            .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

        AuditingConfiguration.ConfigureAppDbContextAuditing();
    }

    public async ValueTask DisposeAsync()
        => await appDbContextFixture.ResetAsync();

    private async Task ClearTableAsync()
    {
        await foreach (var entity in _tableClient.QueryAsync<TableEntity>(cancellationToken: TestContext.Current.CancellationToken))
        {
            await _tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, cancellationToken: TestContext.Current.CancellationToken);
        }
    }

    [Fact(DisplayName = "SaveChanges should audit the membership insert when a bowler gains a membership")]
    public async Task SaveChanges_ShouldAuditMembershipInsert_WhenBowlerGainsMembership()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        bowler.AddMembership(MembershipType.New, new DateOnly(2025, 3, 15)).IsError.ShouldBeFalse();

        await using var dbContext = appDbContextFixture.CreateDbContext(_auditInterceptor);
        await dbContext.Bowlers.AddAsync(bowler, ct);

        // Act
        await dbContext.SaveChangesAsync(ct);

        // Assert
        var entries = await ReadAuditedEntriesAsync();
        entries.ShouldContain(entry => entry.Name == nameof(BowlerMembership) && entry.Action == "Insert");
    }

    private async Task<List<EventEntry>> ReadAuditedEntriesAsync()
    {
        var entries = new List<EventEntry>();

        await foreach (var entity in _tableClient.QueryAsync<TableEntity>(cancellationToken: TestContext.Current.CancellationToken))
        {
            var json = ChunkedAuditEventTableEntity.ReadJson(entity);
            if (json.Length == 0)
            {
                continue;
            }

            var efEvent = Audit.Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(json).GetEntityFrameworkEvent();
            if (efEvent is not null)
            {
                entries.AddRange(efEvent.Entries);
            }
        }

        return entries;
    }
}
