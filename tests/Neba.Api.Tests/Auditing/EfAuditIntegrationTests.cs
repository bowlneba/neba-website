using Audit.Core;
using Audit.EntityFramework;

using Azure.Data.Tables;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Tournaments;

using Shouldly;

namespace Neba.Api.Tests.Auditing;

[IntegrationTest]
[Component("Auditing")]
[Collection("AuditConfigurationSequential")]
public sealed class EfAuditIntegrationTests(AppDbContextFixture appDbContextFixture, AzuriteFixture azuriteFixture)
    : IClassFixture<AppDbContextFixture>, IClassFixture<AzuriteFixture>, IAsyncLifetime
{
    private const string TableName = "EFAuditEvents";

    private readonly AuditSaveChangesInterceptor _auditInterceptor = new();

    private TableClient _tableClient = null!;

    public async ValueTask InitializeAsync()
    {
        await appDbContextFixture.ResetAsync();

        _tableClient = new TableClient(azuriteFixture.ConnectionString, TableName);
        await _tableClient.CreateIfNotExistsAsync(TestContext.Current.CancellationToken);

        Audit.Core.Configuration.Setup()
            .UseAzureTableStorage(config => config
                .ConnectionString(azuriteFixture.ConnectionString)
                .TableName(_ => TableName)
                .EntityBuilder(entity => entity
                    .PartitionKey(ev => ev.EventType ?? "unknown")
                    .RowKey(_ => Ulid.NewUlid().ToString())))
            .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

        Audit.EntityFramework.Configuration.Setup()
            .ForContext<AppDbContext>(auditConfig => auditConfig
                .AuditEventType("EF:{context}")
                .IncludeEntityObjects(false))
            .UseOptIn()
            .Include<Bowler>();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact(DisplayName = "Saving a change to an audited table should write an EF audit event")]
    public async Task SaveChanges_ShouldWriteAuditEvent_WhenTableIsAudited()
    {
        // Arrange
        await using var dbContext = appDbContextFixture.CreateDbContext(_auditInterceptor);
        dbContext.Bowlers.Add(BowlerFactory.Create());

        // Act
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var eventCount = await CountEventsAsync();
        eventCount.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Saving a change to a non-audited table should not write an audit event")]
    public async Task SaveChanges_ShouldNotWriteAuditEvent_WhenTableIsNotAudited()
    {
        // Arrange
        await using var dbContext = appDbContextFixture.CreateDbContext(_auditInterceptor);
        dbContext.OilPatterns.Add(OilPatternFactory.Create());

        // Act
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var eventCount = await CountEventsAsync();
        eventCount.ShouldBe(0);
    }

    private async Task<int> CountEventsAsync()
    {
        var count = 0;

        await foreach (var _ in _tableClient.QueryAsync<TableEntity>(cancellationToken: TestContext.Current.CancellationToken))
        {
            count++;
        }

        return count;
    }
}
