using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;

using Neba.Api.BackgroundJobs;
using Neba.Api.Discord;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.BackgroundJobs;

[UnitTest]
[Component("Infrastructure.BackgroundJobs")]
public sealed class DiscordJobFailureFilterTests
{
    // Stand-in job type/method for building a Hangfire Job/PerformContext in tests. Nested (rather
    // than a method on the test class itself) so xUnit doesn't flag it as an undecorated test method,
    // and public because Hangfire's Job constructor refuses non-public methods.
    private static class SampleJob
    {
        public static void SampleJobMethod()
        {
            // Stand-in job method; never actually invoked in these tests.
        }
    }

    private static PerformedContext CreatePerformedContext(Exception? exception, bool exceptionHandled = false)
    {
        var storage = new Mock<JobStorage>(MockBehavior.Strict).Object;
        var connection = new Mock<IStorageConnection>(MockBehavior.Strict).Object;
        var cancellationToken = new Mock<IJobCancellationToken>(MockBehavior.Strict).Object;

        var job = new Job(typeof(SampleJob), typeof(SampleJob).GetMethod(nameof(SampleJob.SampleJobMethod))!);
        var backgroundJob = new BackgroundJob("1", job, DateTime.UtcNow);

        var performContext = new PerformContext(storage, connection, backgroundJob, cancellationToken);

        return new PerformedContext(performContext, null, canceled: false, exception)
        {
            ExceptionHandled = exceptionHandled
        };
    }

    [Fact(DisplayName = "OnPerformed should post a Discord alert when the job failed with an unhandled exception")]
    public void OnPerformed_ShouldPostDiscordAlert_WhenJobFailedWithUnhandledException()
    {
        // Arrange
        var exception = new InvalidOperationException("Boom");
        var context = CreatePerformedContext(exception);

        var discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        DiscordAlert? postedAlert = null;
        discordNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()))
            .Callback<DiscordAlert, CancellationToken>((alert, _) => postedAlert = alert)
            .Returns(Task.CompletedTask)
            .Verifiable();

        var filter = new DiscordJobFailureFilter(discordNotifier.Object);

        // Act
        filter.OnPerformed(context);

        // Assert
        postedAlert.ShouldNotBeNull();
        postedAlert.Severity.ShouldBe(DiscordAlertSeverity.Warning);
        postedAlert.Title.ShouldBe("Recurring job failed");
        postedAlert.Body.ShouldBe("Boom");
        postedAlert.Metadata.ShouldNotBeNull();
        postedAlert.Metadata["JobName"].ShouldBe(nameof(SampleJob.SampleJobMethod));
        discordNotifier.VerifyAll();
    }

    [Fact(DisplayName = "OnPerformed should not post a Discord alert when the job succeeded")]
    public void OnPerformed_ShouldNotPostDiscordAlert_WhenJobSucceeded()
    {
        // Arrange
        var context = CreatePerformedContext(exception: null);
        var discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        var filter = new DiscordJobFailureFilter(discordNotifier.Object);

        // Act
        filter.OnPerformed(context);

        // Assert
        discordNotifier.Verify(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "OnPerformed should not post a Discord alert when the exception was already handled by another filter")]
    public void OnPerformed_ShouldNotPostDiscordAlert_WhenExceptionAlreadyHandled()
    {
        // Arrange
        var exception = new InvalidOperationException("Boom");
        var context = CreatePerformedContext(exception, exceptionHandled: true);
        var discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        var filter = new DiscordJobFailureFilter(discordNotifier.Object);

        // Act
        filter.OnPerformed(context);

        // Assert
        discordNotifier.Verify(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "OnPerforming should not throw")]
    public void OnPerforming_ShouldNotThrow()
    {
        // Arrange
        var context = CreatePerformedContext(exception: null);
        var performingContext = new PerformingContext(context);
        var discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        var filter = new DiscordJobFailureFilter(discordNotifier.Object);

        // Act & Assert
        Should.NotThrow(() => filter.OnPerforming(performingContext));
    }
}
