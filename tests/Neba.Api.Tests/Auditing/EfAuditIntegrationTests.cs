using Audit.Core;

using Azure.Data.Tables;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Auditing;
using Neba.Api.Database;
using Neba.Api.Database.Interceptors;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Series;
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
                .EntityBuilder(entity => entity
                    .PartitionKey(ev => ev.EventType ?? "unknown")
                    .RowKey(_ => Ulid.NewUlid().ToString())))
            .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

        Audit.EntityFramework.Configuration.Setup()
            .ForContext<AppDbContext>(auditConfig => auditConfig
                .AuditEventType("EF:{context}")
                .IncludeEntityObjects(true))
            .UseOptIn()
            .Include<Bowler>()
            .Include<Series>()
            .Include<SeriesStage>();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact(DisplayName = "Saving a change to an audited table should write an EF audit event")]
    public async Task SaveChanges_ShouldWriteAuditEvent_WhenTableIsAudited()
    {
        // Arrange
        await using var dbContext = appDbContextFixture.CreateDbContext(_auditInterceptor);
        await dbContext.Bowlers.AddAsync(BowlerFactory.Create(), TestContext.Current.CancellationToken);

        // Act
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var eventCount = await CountEventsAsync();
        eventCount.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Saving a Series should write an EF audit event")]
    public async Task SaveChanges_ShouldWriteAuditEvents_WhenSeriesAndStagesAreSaved()
    {
        // Arrange
        var season = SeasonFactory.Create();
        var tournament = TournamentFactory.Create(seasonId: season.Id);
        await using (var seed = appDbContextFixture.CreateDbContext())
        {
            seed.Seasons.Add(season);
            seed.Tournaments.Add(tournament);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await ClearTableAsync();

        var series = SeriesFactory.CreateFor(season, [(tournament, StageKind.Qualifying, 8, null, null)]);
        await using var dbContext = appDbContextFixture.CreateDbContext(_auditInterceptor);
        dbContext.Series.Add(series);

        // Act
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var eventCount = await CountEventsAsync();
        eventCount.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Adding a Stage override should write an EF audit event under the real opt-in list")]
    public async Task SaveChanges_ShouldWriteAuditEvent_WhenStageOverrideIsAdded()
    {
        // Arrange
        var season = SeasonFactory.Create();
        var tournament = TournamentFactory.Create(seasonId: season.Id);
        var series = SeriesFactory.CreateFor(season, [(tournament, StageKind.Qualifying, 8, null, null)]);
        var bowler = BowlerFactory.Create();
        await using (var seed = appDbContextFixture.CreateDbContext())
        {
            seed.Seasons.Add(season);
            seed.Tournaments.Add(tournament);
            seed.Series.Add(series);
            seed.Bowlers.Add(bowler);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await ClearTableAsync();

        // The real opt-in list, not this class's shortened one, which leaves StageOverride out.
        AuditingConfiguration.ConfigureAppDbContextAuditing();

        await using var dbContext = appDbContextFixture.CreateDbContext(_auditInterceptor);
        var stored = await dbContext.Series
            .Include(s => s.Stages)
            .SingleAsync(s => s.Id == series.Id, TestContext.Current.CancellationToken);
        stored.Stages.Single().AddOverride(StageOverrideFactory.Create(bowlerId: bowler.Id));

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
        // SideCut sits in neither this class's opt-in list nor the real one, which an earlier test may leave applied.
        await dbContext.SideCuts.AddAsync(SideCutFactory.Create(), TestContext.Current.CancellationToken);

        // Act
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var eventCount = await CountEventsAsync();
        eventCount.ShouldBe(0);
    }

    [Fact(DisplayName = "Two DbContext instances sharing one interceptor should each write their own EF audit event")]
    public async Task SaveChanges_ShouldWriteAuditEventPerSave_WhenTwoDbContextsShareOneInterceptor()
    {
        // Arrange - AddDbContextPool builds one shared DbContextOptions, so in production a single
        // PooledAuditSaveChangesInterceptor instance is attached to every pooled AppDbContext. This
        // reproduces that by having two independent DbContext instances share the same interceptor
        // and save concurrently.
        await using var firstDbContext = appDbContextFixture.CreateDbContext(_auditInterceptor);
        await using var secondDbContext = appDbContextFixture.CreateDbContext(_auditInterceptor);

        await firstDbContext.Bowlers.AddAsync(BowlerFactory.Create(), TestContext.Current.CancellationToken);
        await secondDbContext.Bowlers.AddAsync(BowlerFactory.Create(), TestContext.Current.CancellationToken);

        // Act
        var firstSave = firstDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var secondSave = secondDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await Task.WhenAll(firstSave, secondSave);

        // Assert
        var eventCount = await CountEventsAsync();
        eventCount.ShouldBe(2);
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

    private async Task ClearTableAsync()
    {
        await foreach (var entity in _tableClient.QueryAsync<TableEntity>(cancellationToken: TestContext.Current.CancellationToken))
        {
            await _tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, cancellationToken: TestContext.Current.CancellationToken);
        }
    }
}