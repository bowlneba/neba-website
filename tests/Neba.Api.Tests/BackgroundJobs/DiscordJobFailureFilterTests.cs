using System.Reflection;

using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

using Neba.Api.BackgroundJobs;
using Neba.Api.Discord;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.BackgroundJobs;

[UnitTest]
[Component("Infrastructure.BackgroundJobs")]
public sealed class DiscordJobFailureFilterTests
{
    // Stand-in job type/method for building a Hangfire Job/BackgroundJob in tests. Nested (rather
    // than a method on the test class itself) so xUnit doesn't flag it as an undecorated test method,
    // and public because Hangfire's Job constructor refuses non-public methods.
    private static class SampleJob
    {
        public static void SampleJobMethod()
        {
            // Stand-in job method; never actually invoked in these tests.
        }
    }

    // A second stand-in job type/method carrying [SkipDiscordJobFailureAlert] on the method itself,
    // matching how PongJob opts out of this filter's alert.
    private static class SelfAlertingJob
    {
        [SkipDiscordJobFailureAlert]
        public static void SelfAlertingJobMethod()
        {
            // Stand-in job method; never actually invoked in these tests.
        }
    }

    private static ElectStateContext CreateElectStateContext(IState candidateState, MethodInfo? method = null)
    {
        var storage = new Mock<JobStorage>(MockBehavior.Strict).Object;
        var connection = new Mock<IStorageConnection>(MockBehavior.Strict).Object;
        var transaction = new Mock<IWriteOnlyTransaction>(MockBehavior.Strict).Object;

        method ??= typeof(SampleJob).GetMethod(nameof(SampleJob.SampleJobMethod))!;
        var job = new Job(method.DeclaringType!, method);
        var backgroundJob = new BackgroundJob("1", job, DateTime.UtcNow);

        var applyContext = new ApplyStateContext(storage, connection, transaction, backgroundJob, candidateState, oldStateName: ProcessingState.StateName);

        return new ElectStateContext(applyContext);
    }

    [Fact(DisplayName = "OnStateElection should post a Discord alert when the candidate state is Failed")]
    public async Task OnStateElection_ShouldPostDiscordAlert_WhenCandidateStateIsFailed()
    {
        // Arrange
        var exception = new InvalidOperationException("Boom");
        var context = CreateElectStateContext(new FailedState(exception));

        // OnStateElection posts fire-and-forget (see its own doc comment - it must not block the
        // Hangfire worker thread), so the test needs to wait for the background Task rather than
        // asserting immediately after the synchronous call returns.
        var notified = new TaskCompletionSource();
        var discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        DiscordAlert? postedAlert = null;
        discordNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()))
            .Callback<DiscordAlert, CancellationToken>((alert, _) =>
            {
                postedAlert = alert;
                notified.SetResult();
            })
            .Returns(Task.CompletedTask);

        var filter = new DiscordJobFailureFilter(discordNotifier.Object);

        // Act
        filter.OnStateElection(context);
        await notified.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        postedAlert.ShouldNotBeNull();
        postedAlert.Severity.ShouldBe(DiscordAlertSeverity.Warning);
        postedAlert.Title.ShouldBe("Recurring job failed");
        postedAlert.Body.ShouldBe("Boom");
        postedAlert.Metadata.ShouldNotBeNull();
        postedAlert.Metadata["JobName"].ShouldBe(nameof(SampleJob.SampleJobMethod));
    }

    [Fact(DisplayName = "OnStateElection should mask an email address embedded in the exception message before posting to Discord")]
    public async Task OnStateElection_ShouldMaskEmbeddedEmailAddress_InDiscordAlertBody()
    {
        // Arrange
        var exception = new InvalidOperationException("Failed to sync bowler jdoe@example.com");
        var context = CreateElectStateContext(new FailedState(exception));

        var notified = new TaskCompletionSource();
        var discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        DiscordAlert? postedAlert = null;
        discordNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()))
            .Callback<DiscordAlert, CancellationToken>((alert, _) =>
            {
                postedAlert = alert;
                notified.SetResult();
            })
            .Returns(Task.CompletedTask);

        var filter = new DiscordJobFailureFilter(discordNotifier.Object);

        // Act
        filter.OnStateElection(context);
        await notified.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        postedAlert.ShouldNotBeNull();
        postedAlert.Body.ShouldNotContain("jdoe@example.com");
        postedAlert.Body.ShouldBe("Failed to sync bowler j***************");
    }

    [Fact(DisplayName = "OnStateElection should not post a Discord alert when the candidate state is Succeeded")]
    public void OnStateElection_ShouldNotPostDiscordAlert_WhenCandidateStateIsSucceeded()
    {
        // Arrange
        var context = CreateElectStateContext(new SucceededState(result: null, latency: 0, performanceDuration: 0));
        var discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        var filter = new DiscordJobFailureFilter(discordNotifier.Object);

        // Act
        filter.OnStateElection(context);

        // Assert - OnStateElection dispatches its alert via a discarded fire-and-forget Task.Run
        // (see its own doc comment), so a Strict mock with no setup wouldn't reliably fail this
        // test synchronously if it were called - explicit Verify is the only real assertion here.
        discordNotifier.Verify(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "OnStateElection should not post a Discord alert when AutomaticRetryAttribute has already rewritten the candidate state to Scheduled for a retry")]
    public void OnStateElection_ShouldNotPostDiscordAlert_WhenCandidateStateIsScheduledForRetry()
    {
        // Arrange
        // Simulates this filter running after AutomaticRetryAttribute has already elected a retry
        // in place of the original FailedState candidate - the scenario this filter exists to
        // distinguish from a genuinely exhausted-retries failure.
        var context = CreateElectStateContext(new ScheduledState(TimeSpan.FromMinutes(1)));
        var discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        var filter = new DiscordJobFailureFilter(discordNotifier.Object);

        // Act
        filter.OnStateElection(context);

        // Assert - OnStateElection dispatches its alert via a discarded fire-and-forget Task.Run
        // (see its own doc comment), so a Strict mock with no setup wouldn't reliably fail this
        // test synchronously if it were called - explicit Verify is the only real assertion here.
        discordNotifier.Verify(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "OnStateElection should not post a Discord alert when the job method carries [SkipDiscordJobFailureAlert]")]
    public void OnStateElection_ShouldNotPostDiscordAlert_WhenJobMethodCarriesSkipAttribute()
    {
        // Arrange
        var method = typeof(SelfAlertingJob).GetMethod(nameof(SelfAlertingJob.SelfAlertingJobMethod))!;
        var context = CreateElectStateContext(new FailedState(new InvalidOperationException("Boom")), method);
        var discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        var filter = new DiscordJobFailureFilter(discordNotifier.Object);

        // Act
        filter.OnStateElection(context);

        // Assert - OnStateElection dispatches its alert via a discarded fire-and-forget Task.Run
        // (see its own doc comment), so a Strict mock with no setup wouldn't reliably fail this
        // test synchronously if it were called - explicit Verify is the only real assertion here.
        discordNotifier.Verify(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()), Times.Never());
    }
}