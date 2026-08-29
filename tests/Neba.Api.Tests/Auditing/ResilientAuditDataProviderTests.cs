using Audit.Core;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Auditing;
using Neba.Api.Discord;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Auditing;

[UnitTest]
[Component("Auditing")]
public sealed class ResilientAuditDataProviderTests
{
    private static readonly AuditEvent Event = new() { EventType = "Test" };

    private static Mock<IDiscordNotifier> CreateUnusedDiscordNotifier() => new(MockBehavior.Strict);

    // ── InsertEvent ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "InsertEvent returns the inner provider's event id when the inner provider succeeds")]
    public void InsertEvent_WhenInnerProviderSucceeds_ReturnsEventId()
    {
        // Arrange
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.InsertEvent(Event)).Returns("event-id").Verifiable();
        var sut = new ResilientAuditDataProvider(
            inner.Object,
            CreateUnusedDiscordNotifier().Object,
            new FakeLogger<ResilientAuditDataProvider>());

        // Act
        var result = sut.InsertEvent(Event);

        // Assert
        result.ShouldBe("event-id");
        inner.VerifyAll();
    }

    [Fact(DisplayName = "InsertEvent returns null and logs a warning when the inner provider throws, without notifying Discord")]
    public void InsertEvent_WhenInnerProviderThrows_ReturnsNullAndLogsWarningWithoutNotifyingDiscord()
    {
        // Arrange
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.InsertEvent(Event)).Throws(new InvalidOperationException("storage outage"));
        var logger = new FakeLogger<ResilientAuditDataProvider>();
        var sut = new ResilientAuditDataProvider(inner.Object, CreateUnusedDiscordNotifier().Object, logger);

        // Act
        var result = sut.InsertEvent(Event);

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
            .ReturnsAsync("event-id")
            .Verifiable();
        var sut = new ResilientAuditDataProvider(
            inner.Object,
            CreateUnusedDiscordNotifier().Object,
            new FakeLogger<ResilientAuditDataProvider>());

        // Act
        var result = await sut.InsertEventAsync(Event, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe("event-id");
        inner.VerifyAll();
    }

    [Fact(DisplayName = "InsertEventAsync returns null and logs a warning when the inner provider throws")]
    public async Task InsertEventAsync_WhenInnerProviderThrows_ReturnsNullAndLogsWarning()
    {
        // Arrange
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.InsertEventAsync(Event, TestContext.Current.CancellationToken))
            .ThrowsAsync(new InvalidOperationException("storage outage"));
        var logger = new FakeLogger<ResilientAuditDataProvider>();
        var discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        discordNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), TestContext.Current.CancellationToken))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var sut = new ResilientAuditDataProvider(inner.Object, discordNotifier.Object, logger);

        // Act
        var result = await sut.InsertEventAsync(Event, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
        discordNotifier.VerifyAll();
    }

    [Fact(DisplayName = "InsertEventAsync notifies Discord with a warning alert describing the failure when the inner provider throws")]
    public async Task InsertEventAsync_WhenInnerProviderThrows_NotifiesDiscordWithWarningAlert()
    {
        // Arrange
        var exception = new InvalidOperationException("storage outage");
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.InsertEventAsync(Event, TestContext.Current.CancellationToken)).ThrowsAsync(exception);
        DiscordAlert? capturedAlert = null;
        var discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        discordNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), TestContext.Current.CancellationToken))
            .Callback<DiscordAlert, CancellationToken>((alert, _) => capturedAlert = alert)
            .Returns(Task.CompletedTask);
        var sut = new ResilientAuditDataProvider(
            inner.Object,
            discordNotifier.Object,
            new FakeLogger<ResilientAuditDataProvider>());

        // Act
        await sut.InsertEventAsync(Event, TestContext.Current.CancellationToken);

        // Assert
        capturedAlert.ShouldNotBeNull();
        capturedAlert.Severity.ShouldBe(DiscordAlertSeverity.Warning);
        capturedAlert.Title.ShouldBe("Audit event insertion failed");
        capturedAlert.Body.ShouldBe(exception.Message);
        capturedAlert.Metadata.ShouldNotBeNull();
        capturedAlert.Metadata["EventType"].ShouldBe(Event.GetType().FullName);
        capturedAlert.Metadata["ExceptionType"].ShouldBe(exception.GetType().FullName);
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

    [Fact(DisplayName = "ReplaceEvent logs a warning instead of throwing when the inner provider throws")]
    public void ReplaceEvent_WhenInnerProviderThrows_LogsWarningInsteadOfThrowing()
    {
        // Arrange
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.ReplaceEvent("event-id", Event)).Throws(new InvalidOperationException("storage outage"));
        var logger = new FakeLogger<ResilientAuditDataProvider>();
        var sut = new ResilientAuditDataProvider(inner.Object, CreateUnusedDiscordNotifier().Object, logger);

        // Act
        sut.ReplaceEvent("event-id", Event);

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

    [Fact(DisplayName = "ReplaceEventAsync logs a warning instead of throwing when the inner provider throws")]
    public async Task ReplaceEventAsync_WhenInnerProviderThrows_LogsWarningInsteadOfThrowing()
    {
        // Arrange
        var inner = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        inner.Setup(p => p.ReplaceEventAsync("event-id", Event, TestContext.Current.CancellationToken))
            .ThrowsAsync(new InvalidOperationException("storage outage"));
        var logger = new FakeLogger<ResilientAuditDataProvider>();
        var sut = new ResilientAuditDataProvider(inner.Object, CreateUnusedDiscordNotifier().Object, logger);

        // Act
        await sut.ReplaceEventAsync("event-id", Event, TestContext.Current.CancellationToken);

        // Assert
        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }
}