using Azure.Data.Tables;

using Hangfire;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Neba.Api.BackgroundJobs;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

using Npgsql;

using Shouldly;

namespace Neba.Api.Tests.BackgroundJobs;

/// <summary>
/// Exercises the production Hangfire wiring in <see cref="BackgroundJobsConfiguration.AddBackgroundJobs"/> -
/// as opposed to <see cref="AuditJobExecutionIntegrationTests"/>, which only proves the
/// <c>[AuditJobExecutionFilter]</c> attribute itself works when applied to a hand-decorated method.
/// This proves the real <c>.AddAuditJobExecutionFilter(...)</c> global filter registration writes
/// to the real Azure Table Storage-backed data provider for a job with no attribute at all.
/// </summary>
[IntegrationTest]
[Component("Infrastructure.BackgroundJobs")]
[Collection("AuditConfigurationSequential")]
public sealed class HangfireGlobalAuditFilterIntegrationTests(AppDbContextFixture appDbContextFixture, AzuriteFixture azuriteFixture)
    : IClassFixture<AppDbContextFixture>, IClassFixture<AzuriteFixture>, IAsyncLifetime
{
    private const string TableName = "JobAuditEvents";
    private const string SecretArgument = "super-secret-argument";

    private ServiceProvider _serviceProvider = null!;
    private TableClient _tableClient = null!;

    public async ValueTask InitializeAsync()
    {
        await appDbContextFixture.ResetAsync();

        _tableClient = new TableClient(azuriteFixture.ConnectionString, TableName);
        await _tableClient.CreateIfNotExistsAsync(TestContext.Current.CancellationToken);
        await ClearTableAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:tables"] = azuriteFixture.ConnectionString,
                ["Hangfire:WorkerCount"] = "1",
                ["Hangfire:SucceededJobsRetentionDays"] = "1",
                ["Hangfire:DeletedJobsRetentionDays"] = "1",
                ["Hangfire:FailedJobsRetentionDays"] = "1",
                ["Hangfire:AutomaticRetryAttempts"] = "1"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new NpgsqlDataSourceBuilder(appDbContextFixture.ConnectionString).Build());
        services.AddBackgroundJobs(configuration);

        _serviceProvider = services.BuildServiceProvider();

        foreach (var hostedService in _serviceProvider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(TestContext.Current.CancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var hostedService in _serviceProvider.GetServices<IHostedService>())
        {
            await hostedService.StopAsync(TestContext.Current.CancellationToken);
        }

        await _serviceProvider.DisposeAsync();
    }

    [Fact(DisplayName = "The globally registered Hangfire audit filter writes an audit event without job arguments, for a job with no explicit attribute")]
    public async Task Execute_ShouldProduceAuditEvent_ForJobWithNoExplicitAuditAttribute()
    {
        // Arrange
        var client = _serviceProvider.GetRequiredService<IBackgroundJobClient>();

        // Act
        client.Enqueue(() => UnattributedTestJob.Run(SecretArgument));

        // Assert
        var entity = await WaitForEventAsync();
        entity.ShouldNotBeNull();

        var json = entity.GetString("Data") ?? string.Join(string.Empty, entity.Keys.Select(k => entity[k]?.ToString()));
        json.ShouldNotContain(SecretArgument);
    }

    private async Task<TableEntity?> WaitForEventAsync()
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);

        while (DateTimeOffset.UtcNow < deadline)
        {
            await using var enumerator = _tableClient.QueryAsync<TableEntity>(cancellationToken: TestContext.Current.CancellationToken)
                .GetAsyncEnumerator(TestContext.Current.CancellationToken);

            if (await enumerator.MoveNextAsync())
            {
                return enumerator.Current;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken);
        }

        throw new TimeoutException("No audit event was recorded in the JobAuditEvents table within the timeout.");
    }

    private async Task ClearTableAsync()
    {
        await foreach (var entity in _tableClient.QueryAsync<TableEntity>(cancellationToken: TestContext.Current.CancellationToken))
        {
            await _tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, cancellationToken: TestContext.Current.CancellationToken);
        }
    }

    public static class UnattributedTestJob
    {
        public static void Run(string secretArgument)
        {
            // Intentionally empty - only the audit event produced by the global Hangfire
            // filter for this job execution is under test, not the job's own behavior.
        }
    }
}