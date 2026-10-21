using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account.Session;
using Neba.Website.Server.Services;
using Neba.Website.Tests.Account.Session;

namespace Neba.Website.Tests.Services;

[UnitTest]
[Component("Website.Services.CircuitTokenSession")]
public sealed class CircuitTokenSessionTests
{
    private static readonly SessionTokens BrowserTokens = new("user-123", "admin@bowlneba.com", "browser-access", "browser-refresh");

    private readonly Mock<ISessionCookieSync> _cookieSyncMock = new(MockBehavior.Strict);

    [Fact(DisplayName = "Cache should return the circuit's cache")]
    public void Cache_ShouldReturnCircuitCache()
    {
        // Arrange
        var cache = new SessionTokenCache();
        var sut = new CircuitTokenSession(cache, null, _cookieSyncMock.Object);

        // Act
        var result = sut.Cache;

        // Assert
        result.ShouldBeSameAs(cache);
    }

    [Fact(DisplayName = "ReadInitialTokensAsync should read the connection's cookie when a connection context exists")]
    public async Task ReadInitialTokensAsync_ShouldReadConnectionContext_WhenAvailable()
    {
        // Arrange - the strict cookie-sync mock has no setup, so asking the browser would throw.
        var httpContext = SessionHttpContext.Build("user-123", "context-access", "context-refresh");
        var sut = new CircuitTokenSession(new SessionTokenCache(), httpContext, _cookieSyncMock.Object);

        // Act
        var result = await sut.ReadInitialTokensAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(new SessionTokens("user-123", "admin@bowlneba.com", "context-access", "context-refresh"));
    }

    [Fact(DisplayName = "ReadInitialTokensAsync should ask the browser when there is no connection context")]
    public async Task ReadInitialTokensAsync_ShouldReadBrowserCookie_WhenNoConnectionContext()
    {
        // Arrange
        _cookieSyncMock
            .Setup(s => s.ReadAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(BrowserTokens);
        var sut = new CircuitTokenSession(new SessionTokenCache(), null, _cookieSyncMock.Object);

        // Act
        var result = await sut.ReadInitialTokensAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(BrowserTokens);
    }

    [Fact(DisplayName = "ReadLatestCookieTokensAsync should ask the browser even when a connection context exists")]
    public async Task ReadLatestCookieTokensAsync_ShouldAskBrowser_EvenWithConnectionContext()
    {
        // Arrange
        _cookieSyncMock
            .Setup(s => s.ReadAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(BrowserTokens);
        var httpContext = SessionHttpContext.Build("user-123", "stale-access", "stale-refresh");
        var sut = new CircuitTokenSession(new SessionTokenCache(), httpContext, _cookieSyncMock.Object);

        // Act
        var result = await sut.ReadLatestCookieTokensAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(BrowserTokens);
    }

    [Theory(DisplayName = "PersistAsync should return whatever the cookie sync reports")]
    [InlineData(true, TestDisplayName = "Written")]
    [InlineData(false, TestDisplayName = "Rejected")]
    public async Task PersistAsync_ShouldReturnCookieSyncResult(bool persisted)
    {
        // Arrange
        _cookieSyncMock
            .Setup(s => s.PersistAsync(BrowserTokens, TestContext.Current.CancellationToken))
            .ReturnsAsync(persisted);
        var sut = new CircuitTokenSession(new SessionTokenCache(), null, _cookieSyncMock.Object);

        // Act
        var result = await sut.PersistAsync(BrowserTokens, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(persisted);
    }
}