using System.Reflection;

using Hangfire;
using Hangfire.Common;
using Hangfire.InMemory;
using Hangfire.States;
using Hangfire.Storage;

using Neba.Api.BackgroundJobs;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.BackgroundJobs;

[UnitTest]
[Component("Infrastructure.BackgroundJobs")]
public sealed class OptOutAutomaticRetryAttributeTests
{
    [NoAutomaticRetry]
    public sealed class NoRetryArgument
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class OrdinaryArgument
    {
        public string Name { get; set; } = string.Empty;
    }

    private static class SampleJobs
    {
        public static void NoRetryJob(NoRetryArgument argument)
        {
            // Stand-in job method; never actually invoked in these tests.
        }

        public static void OrdinaryJob(OrdinaryArgument argument)
        {
            // Stand-in job method; never actually invoked in these tests.
        }
    }

    private static ElectStateContext CreateElectStateContext(MethodInfo method, object argument)
    {
        var storage = new Mock<JobStorage>(MockBehavior.Strict).Object;
        var connection = new Mock<IStorageConnection>(MockBehavior.Strict);
        // No earlier attempt recorded: Hangfire returns null for a parameter that was never set.
#nullable disable
        connection
            .Setup(c => c.GetJobParameter(It.IsAny<string>(), "RetryCount"))
            .Returns((string)null);
#nullable enable
        connection
            .Setup(c => c.SetJobParameter(It.IsAny<string>(), "RetryCount", It.IsAny<string>()));
        var transaction = new Mock<IWriteOnlyTransaction>(MockBehavior.Strict).Object;

        var job = new Job(method.DeclaringType!, method, argument);
        var backgroundJob = new BackgroundJob("1", job, DateTime.UtcNow);

        var applyContext = new ApplyStateContext(
            storage,
            connection.Object,
            transaction,
            backgroundJob,
            new FailedState(new InvalidOperationException("Boom")),
            oldStateName: ProcessingState.StateName);

        return new ElectStateContext(applyContext);
    }

    [Fact(DisplayName = "OnStateElection should leave the Failed state alone for a job argument marked [NoAutomaticRetry]")]
    public void OnStateElection_ShouldKeepFailedState_WhenArgumentIsMarkedNoAutomaticRetry()
    {
        // Arrange
        var filter = new OptOutAutomaticRetryAttribute(new AutomaticRetryAttribute { Attempts = 3 });
        var context = CreateElectStateContext(
            typeof(SampleJobs).GetMethod(nameof(SampleJobs.NoRetryJob))!,
            new NoRetryArgument());

        // Act
        filter.OnStateElection(context);

        // Assert
        context.CandidateState.ShouldBeOfType<FailedState>();
    }

    [Fact(DisplayName = "OnStateElection should schedule a retry for a job argument without the marker")]
    public void OnStateElection_ShouldScheduleRetry_WhenArgumentIsNotMarked()
    {
        // Arrange
        var filter = new OptOutAutomaticRetryAttribute(new AutomaticRetryAttribute { Attempts = 3 });
        var context = CreateElectStateContext(
            typeof(SampleJobs).GetMethod(nameof(SampleJobs.OrdinaryJob))!,
            new OrdinaryArgument());

        // Act
        filter.OnStateElection(context);

        // Assert
        context.CandidateState.ShouldBeOfType<ScheduledState>();
    }

    [Fact(DisplayName = "Order should match the wrapped AutomaticRetryAttribute")]
    public void Order_ShouldMatchInnerFilter()
    {
        // Arrange
        var inner = new AutomaticRetryAttribute();

        // Act
        var filter = new OptOutAutomaticRetryAttribute(inner);

        // Assert
        filter.Order.ShouldBe(inner.Order);
    }
}

/// <summary>
/// Runs the filter through a real Hangfire worker with in-memory storage and an isolated filter
/// collection, so no process-wide <c>GlobalJobFilters</c> state is touched.
/// </summary>
[IntegrationTest]
[Component("Infrastructure.BackgroundJobs")]
public sealed class OptOutAutomaticRetryAttributePipelineTests : IAsyncLifetime
{
    private InMemoryStorage _storage = null!;
    private BackgroundJobServer _server = null!;
    private BackgroundJobClient _client = null!;

    public ValueTask InitializeAsync()
    {
        var filters = new JobFilterCollection
        {
            new OptOutAutomaticRetryAttribute(new AutomaticRetryAttribute { Attempts = 3 })
        };

        _storage = new InMemoryStorage();
        _client = new BackgroundJobClient(_storage, filters);
        _server = new BackgroundJobServer(
            new BackgroundJobServerOptions
            {
                Queues = ["default"],
                WorkerCount = 1,
                ServerName = nameof(OptOutAutomaticRetryAttributePipelineTests),
                FilterProvider = filters
            },
            _storage);

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _server.Dispose();
        _storage.Dispose();

        return ValueTask.CompletedTask;
    }

    [Fact(DisplayName = "A failing job marked [NoAutomaticRetry] ends Failed after one attempt")]
    public async Task Execute_ShouldFailWithoutRetry_WhenJobIsMarkedNoAutomaticRetry()
    {
        // Arrange & Act
        var jobId = _client.Enqueue(() => PipelineJobs.Fail(new OptOutAutomaticRetryAttributeTests.NoRetryArgument()));
        var state = await WaitForSettledStateAsync(jobId);

        // Assert
        state.ShouldBe(FailedState.StateName);
    }

    [Fact(DisplayName = "A failing job without the marker is scheduled for a retry")]
    public async Task Execute_ShouldScheduleRetry_WhenJobIsNotMarked()
    {
        // Arrange & Act
        var jobId = _client.Enqueue(() => PipelineJobs.FailOrdinary(new OptOutAutomaticRetryAttributeTests.OrdinaryArgument()));
        var state = await WaitForSettledStateAsync(jobId);

        // Assert
        state.ShouldBe(ScheduledState.StateName);
    }

    private async Task<string> WaitForSettledStateAsync(string jobId)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
        string? state = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            using var connection = _storage.GetConnection();
            state = connection.GetJobData(jobId)?.State;

            if (state is "Failed" or "Scheduled" or "Succeeded")
            {
                return state;
            }

            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        throw new TimeoutException($"Job {jobId} did not settle; last state was '{state}'.");
    }

    public static class PipelineJobs
    {
        public static void Fail(OptOutAutomaticRetryAttributeTests.NoRetryArgument argument)
            => throw new InvalidOperationException("Boom");

        public static void FailOrdinary(OptOutAutomaticRetryAttributeTests.OrdinaryArgument argument)
            => throw new InvalidOperationException("Boom");
    }
}
