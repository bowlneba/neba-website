using Audit.Core;
using Audit.Core.Providers;
using Audit.Hangfire;

using Hangfire;
using Hangfire.InMemory;

using Neba.TestFactory.Attributes;

using Shouldly;

namespace Neba.Api.Tests.BackgroundJobs;

[IntegrationTest]
[Component("Infrastructure.BackgroundJobs")]
[Collection("AuditConfigurationSequential")]
public sealed class AuditJobExecutionIntegrationTests : IAsyncLifetime
{
    private const string SecretArgument = "super-secret-argument";

    private InMemoryStorage _storage = null!;
    private BackgroundJobServer _server = null!;

    public ValueTask InitializeAsync()
    {
        Configuration.Setup()
            .Use(new InMemoryDataProvider())
            .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);
        Configuration.ResetCustomActions();

        _storage = new InMemoryStorage();

        _server = new BackgroundJobServer(
            new BackgroundJobServerOptions
            {
                Queues = ["default"],
                WorkerCount = 1,
                ServerName = nameof(AuditJobExecutionIntegrationTests)
            },
            _storage);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _server.Dispose();
        _storage.Dispose();
        Configuration.ResetCustomActions();
        await ValueTask.CompletedTask;
    }

    private static InMemoryDataProvider Provider => (InMemoryDataProvider)Configuration.DataProvider;

    [Fact(DisplayName = "A successful job execution produces an audit event without job arguments")]
    public async Task Execute_ShouldProduceAuditEvent_WhenJobSucceeds()
    {
        // Arrange
        var client = new BackgroundJobClient(_storage);

        // Act
        client.Enqueue(() => AuditableTestJob.Succeed(SecretArgument));

        // Assert
        var auditEvent = await WaitForEventAsync("Job:" + typeof(AuditableTestJob).Name + ".Succeed");

        auditEvent.JobExecution.IsSuccess.ShouldBeTrue();
        auditEvent.JobExecution.Args.ShouldBeNull();
        auditEvent.ToJson().ShouldNotContain(SecretArgument);
    }

    [Fact(DisplayName = "A failed job execution produces an audit event capturing the exception, without job arguments")]
    public async Task Execute_ShouldProduceAuditEvent_WhenJobFails()
    {
        // Arrange
        var client = new BackgroundJobClient(_storage);

        // Act
        client.Enqueue(() => AuditableTestJob.Fail(SecretArgument));

        // Assert
        var auditEvent = await WaitForEventAsync("Job:" + typeof(AuditableTestJob).Name + ".Fail");

        auditEvent.JobExecution.IsSuccess.ShouldBeFalse();
        auditEvent.JobExecution.Exception.ShouldNotBeNullOrEmpty();
        auditEvent.JobExecution.Args.ShouldBeNull();
        auditEvent.ToJson().ShouldNotContain(SecretArgument);
    }

    private static async Task<AuditEventHangfireJobExecution> WaitForEventAsync(string eventType)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);

        while (DateTimeOffset.UtcNow < deadline)
        {
            // EventCreationPolicy.InsertOnStartReplaceOnEnd inserts a start-of-job event before
            // JobExecution.IsSuccess/Exception are known, then replaces it once the job finishes.
            // Match on EndDate too so a poll landing between insert and replace doesn't return the
            // pre-execution snapshot and assert against its (still-default) IsSuccess/Exception.
            var match = Provider.GetAllEvents()
                .OfType<AuditEventHangfireJobExecution>()
                .FirstOrDefault(e => e.EventType == eventType && e.EndDate.HasValue);

            if (match is not null)
            {
                return match;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
        }

        throw new TimeoutException($"No audit event of type '{eventType}' was recorded within the timeout.");
    }

    public static class AuditableTestJob
    {
        public static int SucceedCallCount { get; private set; }

        [AuditJobExecutionFilter(EventType = "Job:{type}.{method}", ExcludeArguments = true)]
        public static void Succeed(string secretArgument) => SucceedCallCount++;

        [AuditJobExecutionFilter(EventType = "Job:{type}.{method}", ExcludeArguments = true)]
        public static void Fail(string secretArgument) => throw new InvalidOperationException("Simulated job failure for audit testing.");
    }
}