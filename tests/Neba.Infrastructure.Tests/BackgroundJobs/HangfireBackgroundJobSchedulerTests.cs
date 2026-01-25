using System.ComponentModel;

using Hangfire;
using Hangfire.InMemory;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Application.BackgroundJobs;
using Neba.Infrastructure.BackgroundJobs;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.BackgroundJobs;

[UnitTest]
[Component("Infrastructure.BackgroundJobs")]
[Collection("HangfireSequential")]
public sealed class HangfireBackgroundJobSchedulerTests : IDisposable
{
    private readonly InMemoryStorage _jobStorage;

    public HangfireBackgroundJobSchedulerTests()
    {
        _jobStorage = new InMemoryStorage();
        JobStorage.Current = _jobStorage;
    }

    public void Dispose()
    {
        _jobStorage.Dispose();
    }

    private sealed record TestBackgroundJob(string Name) : IBackgroundJob
    {
        public string JobName => $"Test Job: {Name}";
    }

    private sealed record SimpleTestJob : IBackgroundJob
    {
        public string JobName => "SimpleTestJob";
    }

    private sealed class TestBackgroundJobHandler : IBackgroundJobHandler<TestBackgroundJob>
    {
        public Task ExecuteAsync(TestBackgroundJob job, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class SimpleJobHandler : IBackgroundJobHandler<SimpleTestJob>
    {
        public Task ExecuteAsync(SimpleTestJob job, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed record FailingJob : IBackgroundJob
    {
        public string JobName => "FailingJob";
    }

    private sealed class FailingJobHandler : IBackgroundJobHandler<FailingJob>
    {
        public Task ExecuteAsync(FailingJob job, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Job execution failed");
        }
    }

    private static HangfireBackgroundJobScheduler CreateScheduler()
    {
        var mockScopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);

        mockScopeFactory
            .Setup(x => x.CreateScope())
            .Returns(() =>
            {
                var mockScope = new Mock<IServiceScope>(MockBehavior.Strict);
                var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);

                mockServiceProvider
                    .Setup(x => x.GetService(typeof(IBackgroundJobHandler<TestBackgroundJob>)))
                    .Returns(new TestBackgroundJobHandler());

                mockServiceProvider
                    .Setup(x => x.GetService(typeof(IBackgroundJobHandler<SimpleTestJob>)))
                    .Returns(new SimpleJobHandler());

                mockServiceProvider
                    .Setup(x => x.GetService(typeof(IBackgroundJobHandler<FailingJob>)))
                    .Returns(new FailingJobHandler());

                mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
                return mockScope.Object;
            });

        return new HangfireBackgroundJobScheduler(
            mockScopeFactory.Object,
            NullLogger<HangfireBackgroundJobScheduler>.Instance);
    }

    [Fact(DisplayName = "Enqueue returns job ID")]
    public void Enqueue_WithValidJob_ReturnsJobId()
    {
        // Arrange
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new TestBackgroundJob("Test Document");

        // Act
        string jobId = scheduler.Enqueue(job);

        // Assert
        jobId.ShouldNotBeNull();
        jobId.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "Enqueue with different job types returns unique job IDs")]
    public void Enqueue_WithDifferentJobTypes_ReturnsUniqueJobIds()
    {
        // Arrange
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job1 = new TestBackgroundJob("Doc1");
        var job2 = new SimpleTestJob();

        // Act
        string jobId1 = scheduler.Enqueue(job1);
        string jobId2 = scheduler.Enqueue(job2);

        // Assert
        jobId1.ShouldNotBeNull();
        jobId2.ShouldNotBeNull();
        jobId1.ShouldNotBe(jobId2);
    }

    [Fact(DisplayName = "Schedule with TimeSpan returns job ID")]
    public void Schedule_WithTimeSpan_ReturnsJobId()
    {
        // Arrange
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new TestBackgroundJob("Delayed Doc");
        var delay = TimeSpan.FromHours(1);

        // Act
        string jobId = scheduler.Schedule(job, delay);

        // Assert
        jobId.ShouldNotBeNull();
        jobId.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "Schedule with DateTimeOffset returns job ID")]
    public void Schedule_WithDateTimeOffset_ReturnsJobId()
    {
        // Arrange
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new TestBackgroundJob("Future Doc");
        DateTimeOffset futureTime = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        string jobId = scheduler.Schedule(job, futureTime);

        // Assert
        jobId.ShouldNotBeNull();
        jobId.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "AddOrUpdateRecurring with cron expression completes successfully")]
    public void AddOrUpdateRecurring_WithCronExpression_Succeeds()
    {
        // Arrange
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new TestBackgroundJob("Recurring Doc");
        const string cronExpression = "0 0 * * *"; // Daily at midnight

        // Act & Assert
        Should.NotThrow(() => scheduler.AddOrUpdateRecurring("recurring_job", job, cronExpression));
    }

    [Fact(DisplayName = "AddOrUpdateRecurring can update existing job")]
    public void AddOrUpdateRecurring_WithSameId_UpdatesJob()
    {
        // Arrange
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job1 = new TestBackgroundJob("Initial");
        var job2 = new TestBackgroundJob("Updated");

        // Act & Assert
        Should.NotThrow(() =>
        {
            scheduler.AddOrUpdateRecurring("updatable", job1, "0 0 * * *");
            scheduler.AddOrUpdateRecurring("updatable", job2, "0 12 * * *");
        });
    }

    [Fact(DisplayName = "RemoveRecurring with valid ID completes successfully")]
    public void RemoveRecurring_WithValidId_Succeeds()
    {
        // Arrange
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new TestBackgroundJob("Removable Job");
        scheduler.AddOrUpdateRecurring("removable_job", job, "0 0 * * *");

        // Act & Assert
        Should.NotThrow(() => scheduler.RemoveRecurring("removable_job"));
    }

    [Fact(DisplayName = "RemoveRecurring with non-existent ID completes successfully")]
    public void RemoveRecurring_WithNonExistentId_Succeeds()
    {
        // Arrange
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();

        // Act & Assert
        Should.NotThrow(() => scheduler.RemoveRecurring("non_existent_job"));
    }

    [Fact(DisplayName = "ContinueJobWith returns job ID")]
    public void ContinueJobWith_WithValidParentJobId_ReturnsJobId()
    {
        // Arrange
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        string parentJobId = scheduler.Enqueue(new SimpleTestJob());
        var job = new TestBackgroundJob("Continuation Job");

        // Act
        string jobId = scheduler.ContinueJobWith(parentJobId, job);

        // Assert
        jobId.ShouldNotBeNull();
        jobId.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "Delete returns result")]
    public void Delete_WithJobId_ReturnsResult()
    {
        // Arrange
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        string jobId = scheduler.Enqueue(new SimpleTestJob());

        // Act
        bool result = scheduler.Delete(jobId);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "Delete with non-existent job ID returns false")]
    public void Delete_WithNonExistentJobId_ReturnsFalse()
    {
        // Arrange
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();

        // Act
        bool result = scheduler.Delete("non_existent_job_id");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "ExecuteJobAsync executes handler successfully")]
    public async Task ExecuteJobAsync_WithValidHandler_ExecutesSuccessfullyAsync()
    {
        // Arrange
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new TestBackgroundJob("Executable Job");

        // Act & Assert
        await Should.NotThrowAsync(() =>
            scheduler.ExecuteJobAsync(job, "Display Name", CancellationToken.None));
    }

    [Fact(DisplayName = "ExecuteJobAsync propagates exception from handler")]
    public async Task ExecuteJobAsync_WhenHandlerThrows_PropagatesExceptionAsync()
    {
        // Arrange
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new FailingJob();

        // Act & Assert
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            scheduler.ExecuteJobAsync(job, "Failing Display", CancellationToken.None));
        exception.Message.ShouldBe("Job execution failed");
    }

    [Fact(DisplayName = "Multiple jobs can be enqueued")]
    public void Enqueue_MultipleJobs_AllReturnUniqueIds()
    {
        // Arrange
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();

        // Act
        string jobId1 = scheduler.Enqueue(new TestBackgroundJob("Job1"));
        string jobId2 = scheduler.Enqueue(new TestBackgroundJob("Job2"));
        string jobId3 = scheduler.Enqueue(new SimpleTestJob());

        // Assert
        jobId1.ShouldNotBeNull();
        jobId2.ShouldNotBeNull();
        jobId3.ShouldNotBeNull();
        new[] { jobId1, jobId2, jobId3 }.ShouldBeUnique();
    }

    [Fact(DisplayName = "Job chain can be created")]
    public void ContinueJobWith_ChainedJobs_ReturnsUniqueIds()
    {
        // Arrange
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();

        // Act
        string jobId1 = scheduler.Enqueue(new TestBackgroundJob("First"));
        string jobId2 = scheduler.ContinueJobWith(jobId1, new TestBackgroundJob("Second"));

        // Assert
        jobId1.ShouldNotBeNull();
        jobId2.ShouldNotBeNull();
        jobId1.ShouldNotBe(jobId2);
    }
}