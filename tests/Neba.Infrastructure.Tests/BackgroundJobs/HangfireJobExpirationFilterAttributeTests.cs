using Hangfire;
using Hangfire.States;
using Hangfire.Storage;

using Neba.Infrastructure.BackgroundJobs;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure.BackgroundJobs;

namespace Neba.Infrastructure.Tests.BackgroundJobs;

[UnitTest]
[Component("Infrastructure.BackgroundJobs")]
public sealed class HangfireJobExpirationFilterAttributeTests
{
    private readonly HangfireSettings _settings;
    private readonly HangfireJobExpirationFilterAttribute _filter;

    public HangfireJobExpirationFilterAttributeTests()
    {
        _settings = HangfireSettingsFactory.Create(
            succeededJobsRetentionDays: 30,
            deletedJobsRetentionDays: 1,
            failedJobsRetentionDays: 7);

        _filter = new HangfireJobExpirationFilterAttribute(_settings);
    }

    [Fact(DisplayName = "Should set expiration timeout for succeeded state")]
    public void OnStateApplied_SucceededState_SetsExpirationTimeout()
    {
        // Arrange
        var storage = new Mock<JobStorage>(MockBehavior.Strict).Object;
        var connection = new Mock<IStorageConnection>(MockBehavior.Strict).Object;
        var transaction = new Mock<IWriteOnlyTransaction>(MockBehavior.Strict).Object;
        var job = new BackgroundJob("1", null, DateTime.UtcNow);
        var newState = new SucceededState(null, 0, 0);

        var context = new ApplyStateContext(
            storage,
            connection,
            transaction,
            job,
            newState,
            null);

        // Act
        _filter.OnStateApplied(context, transaction);

        // Assert
        Assert.Equal(TimeSpan.FromDays(30), context.JobExpirationTimeout);
    }

    [Fact(DisplayName = "Should set expiration timeout for failed state")]
    public void OnStateApplied_FailedState_SetsExpirationTimeout()
    {
        // Arrange
        var storage = new Mock<JobStorage>(MockBehavior.Strict).Object;
        var connection = new Mock<IStorageConnection>(MockBehavior.Strict).Object;
        var transaction = new Mock<IWriteOnlyTransaction>(MockBehavior.Strict).Object;
        var job = new BackgroundJob("1", null, DateTime.UtcNow);
        var exception = new InvalidOperationException("Test failure");
        var newState = new FailedState(exception);

        var context = new ApplyStateContext(
            storage,
            connection,
            transaction,
            job,
            newState,
            null);

        // Act
        _filter.OnStateApplied(context, transaction);

        // Assert
        Assert.Equal(TimeSpan.FromDays(7), context.JobExpirationTimeout);
    }

    [Fact(DisplayName = "Should set expiration timeout for deleted state")]
    public void OnStateApplied_DeletedState_SetsExpirationTimeout()
    {
        // Arrange
        var storage = new Mock<JobStorage>(MockBehavior.Strict).Object;
        var connection = new Mock<IStorageConnection>(MockBehavior.Strict).Object;
        var transaction = new Mock<IWriteOnlyTransaction>(MockBehavior.Strict).Object;
        var job = new BackgroundJob("1", null, DateTime.UtcNow);
        var newState = new DeletedState();

        var context = new ApplyStateContext(
            storage,
            connection,
            transaction,
            job,
            newState,
            null);

        // Act
        _filter.OnStateApplied(context, transaction);

        // Assert
        Assert.Equal(TimeSpan.FromDays(1), context.JobExpirationTimeout);
    }

    [Fact(DisplayName = "Should not set expiration timeout for other states")]
    public void OnStateApplied_OtherState_DoesNotSetExpirationTimeout()
    {
        // Arrange
        var storage = new Mock<JobStorage>(MockBehavior.Strict).Object;
        var connection = new Mock<IStorageConnection>(MockBehavior.Strict).Object;
        var transaction = new Mock<IWriteOnlyTransaction>(MockBehavior.Strict).Object;
        var job = new BackgroundJob("1", null, DateTime.UtcNow);
        var newState = new EnqueuedState();

        var context = new ApplyStateContext(
            storage,
            connection,
            transaction,
            job,
            newState,
            null);

        var initialExpirationTimeout = context.JobExpirationTimeout;

        // Act
        _filter.OnStateApplied(context, transaction);

        // Assert
        Assert.Equal(initialExpirationTimeout, context.JobExpirationTimeout);
    }
}