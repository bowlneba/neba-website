using Audit.Core;

using Azure.Data.Tables;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Auditing;
using Neba.Api.BackgroundJobs;
using Neba.Api.Discord;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.BackgroundJobs;

/// <summary>
/// Covers <see cref="BackgroundJobsConfiguration.CreateJobAuditDataProvider"/> on its own.
/// <see cref="HangfireGlobalAuditFilterIntegrationTests"/> covers the full Hangfire wiring, but it
/// can host only one test per process because Hangfire.Console's <c>UseConsole</c> runs once.
/// </summary>
[IntegrationTest]
[Component("Infrastructure.BackgroundJobs")]
public sealed class JobAuditDataProviderTests(AzuriteFixture azuriteFixture)
    : IClassFixture<AzuriteFixture>
{
    // A unique table name per test: Audit.NET caches its TableClient in a static dictionary keyed by
    // table name alone, so reusing "JobAuditEvents" would bind this test to whichever Azurite
    // container (this class's or HangfireGlobalAuditFilterIntegrationTests's) cached a client first.
    private static string NewTableName() => $"JobAuditTest{Guid.NewGuid():N}";

    [Fact(DisplayName = "InsertEvent should store the event in the job audit table")]
    public async Task InsertEvent_ShouldStoreEvent_WhenEventFits()
    {
        // Arrange
        var tableName = NewTableName();
        var logger = new FakeLogger<ResilientAuditDataProvider>();
        var provider = BackgroundJobsConfiguration.CreateJobAuditDataProvider(
            azuriteFixture.ConnectionString,
            new Mock<IDiscordNotifier>(MockBehavior.Strict).Object,
            logger,
            tableName);

        var eventType = $"Job:Test.{Guid.NewGuid():N}";
        var auditEvent = new AuditEvent { EventType = eventType, CustomFields = [] };

        // Act
        await provider.InsertEventAsync(auditEvent, TestContext.Current.CancellationToken);

        // Assert
        // The resilient wrapper swallows storage failures, so surface any it logged.
        logger.Collector.GetSnapshot().ShouldBeEmpty(
            string.Join(Environment.NewLine, logger.Collector.GetSnapshot().Select(record => record.Exception?.ToString() ?? record.Message)));

        var tableClient = new TableClient(azuriteFixture.ConnectionString, tableName);
        var entities = await tableClient
            .QueryAsync<TableEntity>(entity => entity.PartitionKey == eventType, cancellationToken: TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        ChunkedAuditEventTableEntity.ReadJson(entities.ShouldHaveSingleItem()).ShouldContain(eventType);
    }

    [Fact(DisplayName = "InsertEvent should log a warning and alert Discord instead of throwing when the event cannot be stored")]
    public async Task InsertEvent_ShouldLogAndAlert_WhenEventIsTooLargeToStore()
    {
        // Arrange
        var alertSent = new TaskCompletionSource<DiscordAlert>(TaskCreationOptions.RunContinuationsAsynchronously);
        var discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        discordNotifier
            .Setup(notifier => notifier.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()))
            .Callback<DiscordAlert, CancellationToken>((alert, _) => alertSent.TrySetResult(alert))
            .Returns(Task.CompletedTask);

        var logger = new FakeLogger<ResilientAuditDataProvider>();
        var provider = BackgroundJobsConfiguration.CreateJobAuditDataProvider(azuriteFixture.ConnectionString, discordNotifier.Object, logger, NewTableName());

        var auditEvent = new AuditEvent { EventType = "Job:Test.TooLarge", CustomFields = [] };
        auditEvent.CustomFields["Content"] = new string('a', ChunkedAuditEventTableEntity.MaxChunks * ChunkedAuditEventTableEntity.ChunkSize);

        // Act
        await Should.NotThrowAsync(() => provider.InsertEventAsync(auditEvent, TestContext.Current.CancellationToken));

        // Assert
        logger.Collector.GetSnapshot().ShouldContain(record => record.Level == LogLevel.Warning);

        var alert = await alertSent.Task.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
        alert.Title.ShouldBe("Audit event insertion failed");
    }
}
