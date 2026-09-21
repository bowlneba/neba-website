using System.Diagnostics.CodeAnalysis;

using Audit.Core;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Auditing;
using Neba.Api.Discord;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Auditing;

[UnitTest]
[Component("Auditing")]
public sealed class ResilientAuditDataProviderTests
{
    private static readonly AuditEvent Event = new() { EventType = "Test" };
    private static readonly TimeSpan NotifyWaitTimeout = TimeSpan.FromSeconds(5);

    private static Mock<IDiscordNotifier> CreateUnusedDiscordNotifier() => new(MockBehavior.Strict);

    // Discord notification is fire-and-forget (see ResilientAuditDataProvider.NotifyDiscordFireAndForget's
    // own doc comment - it must not block the audited operation on Discord's own timeout/retry
    // policy), so tests asserting on the notification need to wait for the background Task rather
    // than asserting immediately after the SUT call returns.
    private static Mock<IDiscordNotifier> CreateAwaitableDiscordNotifier(TaskCompletionSource notified, Action<DiscordAlert>? onNotified = null)
    {
        var discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        discordNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), CancellationToken.None))
            .Callback<DiscordAlert, CancellationToken>((alert, _) =>
            {
                onNotified?.Invoke(alert);
                notified.SetResult();
            })
            .Returns(Task.CompletedTask);
        return discordNotifier;
    }

    // ── InsertEvent ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "InsertEvent returns the inner provider's event id when the inner provider succeeds")]
    public void InsertEvent_WhenInnerProviderSucceeds_ReturnsEventId()
    {
        // Arrange
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.InsertEvent(Event)).Returns("event-id");
        var sut = new ResilientAuditDataProvider(
            inner.Object,
            CreateUnusedDiscordNotifier().Object,
            new FakeLogger<ResilientAuditDataProvider>());

        // Act
        var result = sut.InsertEvent(Event);

        // Assert - the returned event id could only come from the mocked call above, so this
        // already proves the inner provider was invoked; no separate Verify needed.
        result.ShouldBe("event-id");
    }

    [Fact(DisplayName = "InsertEvent returns null, logs a warning, and notifies Discord when the inner provider throws")]
    [SuppressMessage("Reliability", "CA1849:Call async methods when in an async method", Justification = "InsertEvent is the sync overload under test here, not a blocking call on an async one; the method is async only to await the fire-and-forget Discord notification below it.")]
    [SuppressMessage("VisualStudio.Threading.Analyzers", "VSTHRD103:Call async methods when in an async method", Justification = "Same as the CA1849 suppression above.")]
    public async Task InsertEvent_WhenInnerProviderThrows_ReturnsNullLogsWarningAndNotifiesDiscord()
    {
        // Arrange
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.InsertEvent(Event)).Throws(new InvalidOperationException("storage outage"));
        var logger = new FakeLogger<ResilientAuditDataProvider>();
        var notified = new TaskCompletionSource();
        var discordNotifier = CreateAwaitableDiscordNotifier(notified);
        var sut = new ResilientAuditDataProvider(inner.Object, discordNotifier.Object, logger);

        // Act
#pragma warning disable S6966 // InsertEvent is the sync overload under test here, not a blocking call on an async one.
        var result = sut.InsertEvent(Event);
#pragma warning restore S6966
        await notified.Task.WaitAsync(NotifyWaitTimeout, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }

    // ── InsertEventAsync ─────────────────────────────────────────────────────

    [Fact(DisplayName = "InsertEventAsync returns the inner provider's event id when the inner provider succeeds")]
    public async Task InsertEventAsync_WhenInnerProviderSucceeds_ReturnsEventId()
    {
        // Arrange
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.InsertEventAsync(Event, TestContext.Current.CancellationToken))
            .ReturnsAsync("event-id");
        var sut = new ResilientAuditDataProvider(
            inner.Object,
            CreateUnusedDiscordNotifier().Object,
            new FakeLogger<ResilientAuditDataProvider>());

        // Act
        var result = await sut.InsertEventAsync(Event, TestContext.Current.CancellationToken);

        // Assert - the returned event id could only come from the mocked call above, so this
        // already proves the inner provider was invoked; no separate Verify needed.
        result.ShouldBe("event-id");
    }

    [Fact(DisplayName = "InsertEventAsync returns null and logs a warning when the inner provider throws")]
    public async Task InsertEventAsync_WhenInnerProviderThrows_ReturnsNullAndLogsWarning()
    {
        // Arrange
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.InsertEventAsync(Event, TestContext.Current.CancellationToken))
            .ThrowsAsync(new InvalidOperationException("storage outage"));
        var logger = new FakeLogger<ResilientAuditDataProvider>();
        var notified = new TaskCompletionSource();
        var discordNotifier = CreateAwaitableDiscordNotifier(notified);
        var sut = new ResilientAuditDataProvider(inner.Object, discordNotifier.Object, logger);

        // Act
        var result = await sut.InsertEventAsync(Event, TestContext.Current.CancellationToken);
        await notified.Task.WaitAsync(NotifyWaitTimeout, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }

    [Fact(DisplayName = "InsertEventAsync notifies Discord with a warning alert describing the failure when the inner provider throws")]
    public async Task InsertEventAsync_WhenInnerProviderThrows_NotifiesDiscordWithWarningAlert()
    {
        // Arrange
        var exception = new InvalidOperationException("storage outage");
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.InsertEventAsync(Event, TestContext.Current.CancellationToken)).ThrowsAsync(exception);
        DiscordAlert? capturedAlert = null;
        var notified = new TaskCompletionSource();
        var discordNotifier = CreateAwaitableDiscordNotifier(notified, alert => capturedAlert = alert);
        var sut = new ResilientAuditDataProvider(
            inner.Object,
            discordNotifier.Object,
            new FakeLogger<ResilientAuditDataProvider>());

        // Act
        await sut.InsertEventAsync(Event, TestContext.Current.CancellationToken);
        await notified.Task.WaitAsync(NotifyWaitTimeout, TestContext.Current.CancellationToken);

        // Assert
        capturedAlert.ShouldNotBeNull();
        capturedAlert.Severity.ShouldBe(DiscordAlertSeverity.Warning);
        capturedAlert.Title.ShouldBe("Audit event insertion failed");
        capturedAlert.Body.ShouldBe(exception.Message);
        capturedAlert.Metadata.ShouldNotBeNull();
        capturedAlert.Metadata["EventType"].ShouldBe(Event.EventType);
        capturedAlert.Metadata["ExceptionType"].ShouldBe(exception.GetType().FullName);
    }

    [Fact(DisplayName = "InsertEventAsync masks an email address embedded in the exception message before posting to Discord")]
    public async Task InsertEventAsync_WhenExceptionMessageEmbedsEmailAddress_MasksItInDiscordAlertBody()
    {
        // Arrange
        var exception = new InvalidOperationException("Duplicate audit entry for bowler jdoe@example.com");
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.InsertEventAsync(Event, TestContext.Current.CancellationToken)).ThrowsAsync(exception);
        DiscordAlert? capturedAlert = null;
        var notified = new TaskCompletionSource();
        var discordNotifier = CreateAwaitableDiscordNotifier(notified, alert => capturedAlert = alert);
        var sut = new ResilientAuditDataProvider(
            inner.Object,
            discordNotifier.Object,
            new FakeLogger<ResilientAuditDataProvider>());

        // Act
        await sut.InsertEventAsync(Event, TestContext.Current.CancellationToken);
        await notified.Task.WaitAsync(NotifyWaitTimeout, TestContext.Current.CancellationToken);

        // Assert
        capturedAlert.ShouldNotBeNull();
        capturedAlert.Body.ShouldNotContain("jdoe@example.com");
        capturedAlert.Body.ShouldBe("Duplicate audit entry for bowler j***************");
    }

    // ── ReplaceEvent ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "ReplaceEvent delegates to the inner provider when it succeeds")]
    public void ReplaceEvent_WhenInnerProviderSucceeds_DelegatesToInnerProvider()
    {
        // Arrange
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.ReplaceEvent("event-id", Event)).Verifiable();
        var sut = new ResilientAuditDataProvider(
            inner.Object,
            CreateUnusedDiscordNotifier().Object,
            new FakeLogger<ResilientAuditDataProvider>());

        // Act
        sut.ReplaceEvent("event-id", Event);

        // Assert
        inner.VerifyAll();
    }

    [Fact(DisplayName = "ReplaceEvent logs a warning and notifies Discord instead of throwing when the inner provider throws")]
    [SuppressMessage("Reliability", "CA1849:Call async methods when in an async method", Justification = "ReplaceEvent is the sync overload under test here, not a blocking call on an async one; the method is async only to await the fire-and-forget Discord notification below it.")]
    [SuppressMessage("VisualStudio.Threading.Analyzers", "VSTHRD103:Call async methods when in an async method", Justification = "Same as the CA1849 suppression above.")]
    public async Task ReplaceEvent_WhenInnerProviderThrows_LogsWarningAndNotifiesDiscordInsteadOfThrowing()
    {
        // Arrange
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.ReplaceEvent("event-id", Event)).Throws(new InvalidOperationException("storage outage"));
        var logger = new FakeLogger<ResilientAuditDataProvider>();
        var notified = new TaskCompletionSource();
        var discordNotifier = CreateAwaitableDiscordNotifier(notified);
        var sut = new ResilientAuditDataProvider(inner.Object, discordNotifier.Object, logger);

        // Act
#pragma warning disable S6966 // ReplaceEvent is the sync overload under test here, not a blocking call on an async one.
        sut.ReplaceEvent("event-id", Event);
#pragma warning restore S6966
        await notified.Task.WaitAsync(NotifyWaitTimeout, TestContext.Current.CancellationToken);

        // Assert
        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }

    // ── ReplaceEventAsync ────────────────────────────────────────────────────

    [Fact(DisplayName = "ReplaceEventAsync delegates to the inner provider when it succeeds")]
    public async Task ReplaceEventAsync_WhenInnerProviderSucceeds_DelegatesToInnerProvider()
    {
        // Arrange
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.ReplaceEventAsync("event-id", Event, TestContext.Current.CancellationToken))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var sut = new ResilientAuditDataProvider(
            inner.Object,
            CreateUnusedDiscordNotifier().Object,
            new FakeLogger<ResilientAuditDataProvider>());

        // Act
        await sut.ReplaceEventAsync("event-id", Event, TestContext.Current.CancellationToken);

        // Assert
        inner.VerifyAll();
    }

    [Fact(DisplayName = "ReplaceEventAsync logs a warning and notifies Discord instead of throwing when the inner provider throws")]
    public async Task ReplaceEventAsync_WhenInnerProviderThrows_LogsWarningAndNotifiesDiscordInsteadOfThrowing()
    {
        // Arrange
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.ReplaceEventAsync("event-id", Event, TestContext.Current.CancellationToken))
            .ThrowsAsync(new InvalidOperationException("storage outage"));
        var logger = new FakeLogger<ResilientAuditDataProvider>();
        var notified = new TaskCompletionSource();
        var discordNotifier = CreateAwaitableDiscordNotifier(notified);
        var sut = new ResilientAuditDataProvider(inner.Object, discordNotifier.Object, logger);

        // Act
        await sut.ReplaceEventAsync("event-id", Event, TestContext.Current.CancellationToken);
        await notified.Task.WaitAsync(NotifyWaitTimeout, TestContext.Current.CancellationToken);

        // Assert
        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }

    [Fact(DisplayName = "InsertEventAsync notifies Discord with CancellationToken.None even when the ambient token is already canceled")]
    public async Task InsertEventAsync_WhenAmbientTokenIsCanceled_StillNotifiesDiscordWithoutThrowing()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.InsertEventAsync(Event, cts.Token)).ThrowsAsync(new InvalidOperationException("storage outage"));
        var notified = new TaskCompletionSource();
        var discordNotifier = CreateAwaitableDiscordNotifier(notified);
        var sut = new ResilientAuditDataProvider(
            inner.Object,
            discordNotifier.Object,
            new FakeLogger<ResilientAuditDataProvider>());

        // Act
        var result = await sut.InsertEventAsync(Event, cts.Token);
        await notified.Task.WaitAsync(NotifyWaitTimeout, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    // ── Discord alert cooldown ───────────────────────────────────────────────

    // Counts notifications and signals each one, so a test can wait for the alerts it expects and
    // then check that no further alert arrives.
    private static Mock<IDiscordNotifier> CreateCountingDiscordNotifier(SemaphoreSlim signal, List<DiscordAlert> alerts)
    {
        var discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        discordNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), CancellationToken.None))
            .Callback<DiscordAlert, CancellationToken>((alert, _) =>
            {
                lock (alerts)
                {
                    alerts.Add(alert);
                }

                signal.Release();
            })
            .Returns(Task.CompletedTask);
        return discordNotifier;
    }

    private static Mock<IAuditDataProvider> CreateFailingInnerProvider()
    {
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage outage"));
        return inner;
    }

    [Fact(DisplayName = "InsertEventAsync logs every failure but notifies Discord once for the same event type within the cooldown")]
    public async Task InsertEventAsync_WhenSameEventTypeFailsRepeatedlyWithinCooldown_LogsEachFailureAndNotifiesDiscordOnce()
    {
        // Arrange
        var logger = new FakeLogger<ResilientAuditDataProvider>();
        using var signal = new SemaphoreSlim(0);
        var alerts = new List<DiscordAlert>();
        var sut = new ResilientAuditDataProvider(
            CreateFailingInnerProvider().Object,
            CreateCountingDiscordNotifier(signal, alerts).Object,
            logger,
            new FakeTimeProvider());

        // Act
        await sut.InsertEventAsync(Event, TestContext.Current.CancellationToken);
        await sut.InsertEventAsync(Event, TestContext.Current.CancellationToken);
        await sut.InsertEventAsync(Event, TestContext.Current.CancellationToken);
        await signal.WaitAsync(NotifyWaitTimeout, TestContext.Current.CancellationToken);

        // Assert
        logger.Collector.GetSnapshot().Count.ShouldBe(3);
        (await signal.WaitAsync(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken)).ShouldBeFalse();
        alerts.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "InsertEventAsync notifies Discord again for the same event type once the cooldown has passed")]
    public async Task InsertEventAsync_WhenCooldownHasPassed_NotifiesDiscordAgain()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        using var signal = new SemaphoreSlim(0);
        var alerts = new List<DiscordAlert>();
        var sut = new ResilientAuditDataProvider(
            CreateFailingInnerProvider().Object,
            CreateCountingDiscordNotifier(signal, alerts).Object,
            new FakeLogger<ResilientAuditDataProvider>(),
            timeProvider);

        // Act
        await sut.InsertEventAsync(Event, TestContext.Current.CancellationToken);
        await signal.WaitAsync(NotifyWaitTimeout, TestContext.Current.CancellationToken);
        timeProvider.Advance(ResilientAuditDataProvider.AlertCooldown);
        await sut.InsertEventAsync(Event, TestContext.Current.CancellationToken);
        await signal.WaitAsync(NotifyWaitTimeout, TestContext.Current.CancellationToken);

        // Assert
        alerts.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "InsertEventAsync notifies Discord separately for each event type within the cooldown")]
    public async Task InsertEventAsync_WhenDifferentEventTypesFail_NotifiesDiscordForEach()
    {
        // Arrange
        using var signal = new SemaphoreSlim(0);
        var alerts = new List<DiscordAlert>();
        var sut = new ResilientAuditDataProvider(
            CreateFailingInnerProvider().Object,
            CreateCountingDiscordNotifier(signal, alerts).Object,
            new FakeLogger<ResilientAuditDataProvider>(),
            new FakeTimeProvider());

        // Act
        await sut.InsertEventAsync(new AuditEvent { EventType = "First" }, TestContext.Current.CancellationToken);
        await sut.InsertEventAsync(new AuditEvent { EventType = "Second" }, TestContext.Current.CancellationToken);
        await signal.WaitAsync(NotifyWaitTimeout, TestContext.Current.CancellationToken);
        await signal.WaitAsync(NotifyWaitTimeout, TestContext.Current.CancellationToken);

        // Assert
        alerts.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "InsertEventAsync and ReplaceEventAsync failures for the same event type each notify Discord within the cooldown")]
    public async Task InsertEventAsyncAndReplaceEventAsync_WhenBothFailForSameEventType_NotifyDiscordForEach()
    {
        // Arrange
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage outage"));
        inner.Setup(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage outage"));
        using var signal = new SemaphoreSlim(0);
        var alerts = new List<DiscordAlert>();
        var sut = new ResilientAuditDataProvider(
            inner.Object,
            CreateCountingDiscordNotifier(signal, alerts).Object,
            new FakeLogger<ResilientAuditDataProvider>(),
            new FakeTimeProvider());

        // Act
        await sut.InsertEventAsync(Event, TestContext.Current.CancellationToken);
        await sut.ReplaceEventAsync("event-id", Event, TestContext.Current.CancellationToken);
        await signal.WaitAsync(NotifyWaitTimeout, TestContext.Current.CancellationToken);
        await signal.WaitAsync(NotifyWaitTimeout, TestContext.Current.CancellationToken);

        // Assert
        alerts.Select(a => a.Title).Order().ShouldBe(["Audit event insertion failed", "Audit event replacement failed"]);
    }
}
