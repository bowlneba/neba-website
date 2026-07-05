using Audit.Core;

using Neba.Api.Auditing;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Auditing;

[UnitTest]
[Component("Auditing")]
public sealed class SecurityAuditDataProviderRouterTests
{
    private static readonly AuditEvent SecurityEvent = new() { EventType = "EF:SecurityDbContext" };
    private static readonly AuditEvent OtherEvent = new() { EventType = "EF:AppDbContext" };

    // ── InsertEvent ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "InsertEvent routes a SecurityDbContext event to the security provider")]
    public void InsertEvent_ShouldRouteToSecurityProvider_WhenEventIsFromSecurityDbContext()
    {
        // Arrange
        var securityProvider = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        securityProvider.Setup(p => p.InsertEvent(SecurityEvent)).Returns("event-id").Verifiable();
        var defaultProvider = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        var sut = new SecurityAuditDataProviderRouter(securityProvider.Object, defaultProvider.Object);

        // Act
        var result = sut.InsertEvent(SecurityEvent);

        // Assert
        result.ShouldBe("event-id");
        securityProvider.VerifyAll();
    }

    [Fact(DisplayName = "InsertEvent routes a non-SecurityDbContext event to the default provider")]
    public void InsertEvent_ShouldRouteToDefaultProvider_WhenEventIsNotFromSecurityDbContext()
    {
        // Arrange
        var securityProvider = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        var defaultProvider = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        defaultProvider.Setup(p => p.InsertEvent(OtherEvent)).Returns("event-id").Verifiable();
        var sut = new SecurityAuditDataProviderRouter(securityProvider.Object, defaultProvider.Object);

        // Act
        var result = sut.InsertEvent(OtherEvent);

        // Assert
        result.ShouldBe("event-id");
        defaultProvider.VerifyAll();
    }

    // ── InsertEventAsync ─────────────────────────────────────────────────────

    [Fact(DisplayName = "InsertEventAsync routes a SecurityDbContext event to the security provider")]
    public async Task InsertEventAsync_ShouldRouteToSecurityProvider_WhenEventIsFromSecurityDbContext()
    {
        // Arrange
        var securityProvider = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        securityProvider.Setup(p => p.InsertEventAsync(SecurityEvent, TestContext.Current.CancellationToken))
            .ReturnsAsync("event-id")
            .Verifiable();
        var defaultProvider = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        var sut = new SecurityAuditDataProviderRouter(securityProvider.Object, defaultProvider.Object);

        // Act
        var result = await sut.InsertEventAsync(SecurityEvent, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe("event-id");
        securityProvider.VerifyAll();
    }

    [Fact(DisplayName = "InsertEventAsync routes a non-SecurityDbContext event to the default provider")]
    public async Task InsertEventAsync_ShouldRouteToDefaultProvider_WhenEventIsNotFromSecurityDbContext()
    {
        // Arrange
        var securityProvider = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        var defaultProvider = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        defaultProvider.Setup(p => p.InsertEventAsync(OtherEvent, TestContext.Current.CancellationToken))
            .ReturnsAsync("event-id")
            .Verifiable();
        var sut = new SecurityAuditDataProviderRouter(securityProvider.Object, defaultProvider.Object);

        // Act
        var result = await sut.InsertEventAsync(OtherEvent, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe("event-id");
        defaultProvider.VerifyAll();
    }

    // ── ReplaceEvent ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "ReplaceEvent routes a SecurityDbContext event to the security provider")]
    public void ReplaceEvent_ShouldRouteToSecurityProvider_WhenEventIsFromSecurityDbContext()
    {
        // Arrange
        var securityProvider = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        securityProvider.Setup(p => p.ReplaceEvent("event-id", SecurityEvent)).Verifiable();
        var defaultProvider = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        var sut = new SecurityAuditDataProviderRouter(securityProvider.Object, defaultProvider.Object);

        // Act
        sut.ReplaceEvent("event-id", SecurityEvent);

        // Assert
        securityProvider.VerifyAll();
    }

    [Fact(DisplayName = "ReplaceEvent routes a non-SecurityDbContext event to the default provider")]
    public void ReplaceEvent_ShouldRouteToDefaultProvider_WhenEventIsNotFromSecurityDbContext()
    {
        // Arrange
        var securityProvider = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        var defaultProvider = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        defaultProvider.Setup(p => p.ReplaceEvent("event-id", OtherEvent)).Verifiable();
        var sut = new SecurityAuditDataProviderRouter(securityProvider.Object, defaultProvider.Object);

        // Act
        sut.ReplaceEvent("event-id", OtherEvent);

        // Assert
        defaultProvider.VerifyAll();
    }

    // ── ReplaceEventAsync ────────────────────────────────────────────────────

    [Fact(DisplayName = "ReplaceEventAsync routes a SecurityDbContext event to the security provider")]
    public async Task ReplaceEventAsync_ShouldRouteToSecurityProvider_WhenEventIsFromSecurityDbContext()
    {
        // Arrange
        var securityProvider = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        securityProvider.Setup(p => p.ReplaceEventAsync("event-id", SecurityEvent, TestContext.Current.CancellationToken))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var defaultProvider = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        var sut = new SecurityAuditDataProviderRouter(securityProvider.Object, defaultProvider.Object);

        // Act
        await sut.ReplaceEventAsync("event-id", SecurityEvent, TestContext.Current.CancellationToken);

        // Assert
        securityProvider.VerifyAll();
    }

    [Fact(DisplayName = "ReplaceEventAsync routes a non-SecurityDbContext event to the default provider")]
    public async Task ReplaceEventAsync_ShouldRouteToDefaultProvider_WhenEventIsNotFromSecurityDbContext()
    {
        // Arrange
        var securityProvider = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        var defaultProvider = new Mock<IAuditDataProvider>(MockBehavior.Strict);
        defaultProvider.Setup(p => p.ReplaceEventAsync("event-id", OtherEvent, TestContext.Current.CancellationToken))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var sut = new SecurityAuditDataProviderRouter(securityProvider.Object, defaultProvider.Object);

        // Act
        await sut.ReplaceEventAsync("event-id", OtherEvent, TestContext.Current.CancellationToken);

        // Assert
        defaultProvider.VerifyAll();
    }
}