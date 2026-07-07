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
        try
        {
            foreach (var hostedService in _serviceProvider.GetServices<IHostedService>())
            {
                await hostedService.StopAsync(TestContext.Current.CancellationToken);
            }

            await _serviceProvider.DisposeAsync();
        }
        finally
        {
            // AddBackgroundJobs(...) in InitializeAsync registers several filters (AutomaticRetryAttribute,
            // HangfireJobExpirationFilterAttribute, AuditJobExecutionFilterAttribute via
            // AddAuditJobExecutionFilter/IGlobalConfiguration.UseFilter) into Hangfire's static, process-wide
            // GlobalJobFilters.Filters collection - none of it scoped to this test's _serviceProvider/storage.
            // Left registered, ALL of these keep firing for every job on every Hangfire server in the process
            // for the rest of the run, including AuditJobExecutionIntegrationTests's own InMemoryStorage-backed
            // server: the leftover AuditJobExecutionFilterAttribute can double-apply alongside that job's own
            // [AuditJobExecutionFilter] attribute (Audit.Hangfire keys IAuditScope into PerformContext.Items
            // via fixed strings, not per-instance ones, so two active instances clobber each other's entry),
            // and - more subtly - the leftover AutomaticRetryAttribute means ANY transient exception from ANY
            // leftover filter (or Hangfire itself) on that job silently triggers a retry, producing a second,
            // separate completed audit event that can be picked up instead of the real outcome. This test is
            // the only one in the suite that populates GlobalJobFilters.Filters, so a full Clear() here is safe
            // and doesn't risk removing something another test depends on. Must run even if StopAsync/
            // DisposeAsync above throws, so it's in `finally` rather than after them unconditionally.
            GlobalJobFilters.Filters.Clear();

            // Hangfire.AspNetCore's AddHangfire/AddHangfireServer wire Hangfire's static, process-wide
            // LogProvider to an AspNetCoreLogProvider backed by this container's ILoggerFactory. Once the
            // container above is disposed, that ILoggerFactory is disposed too, but the static reference
            // survives - so any later Hangfire storage construction anywhere in the test process (e.g.
            // AuditJobExecutionIntegrationTests's `new InMemoryStorage()`) throws ObjectDisposedException.
            // Resetting to null makes Hangfire re-resolve its log provider on next use instead of reusing
            // the disposed one.
            Hangfire.Logging.LogProvider.SetCurrentLogProvider(null!);

            // Hangfire.AspNetCore also wires Hangfire's static, process-wide JobActivator.Current to an
            // AspNetCoreJobActivator bound to THIS test's _serviceProvider. Once that provider is disposed
            // above, JobActivator.Current keeps pointing at it - and every job on every Hangfire server in
            // the process afterward calls the ambient JobActivator.Current.BeginScope(...) to construct the
            // job instance, throwing ObjectDisposedException before the job body ever runs. This is what was
            // actually causing AuditJobExecutionIntegrationTests's "successful" job to record IsSuccess=false:
            // BeginScope failed before AuditableTestJob.Succeed() ever executed, and Hangfire's (now-cleared)
            // AutomaticRetryAttribute retried it - but every retry hit the same disposed provider and failed
            // identically, so even the "final" attempt recorded a failure. Resetting to the default activator
            // makes Hangfire re-resolve job instances via Activator.CreateInstance instead of the disposed DI
            // container.
            JobActivator.Current = new JobActivator();
        }
    }

    [Fact(DisplayName = "The globally registered Hangfire audit filter writes an audit event without job arguments, for a job with no explicit attribute")]
    public async Task Execute_ShouldProduceAuditEvent_ForJobWithNoExplicitAuditAttribute()
    {
        // Arrange
        var client = _serviceProvider.GetRequiredService<IBackgroundJobClient>();

        // Act
        client.Enqueue(() => UnattributedTestJob.Run());

        // Assert
        var entity = await WaitForEventAsync();
        entity.ShouldNotBeNull();

        var json = entity.GetString("Data") ?? string.Concat(entity.Keys.Select(k => entity[k]?.ToString()));
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
        public static void Run()
        {
            // Intentionally empty - only the audit event produced by the global Hangfire
            // filter for this job execution is under test, not the job's own behavior.
        }
    }
}