using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account.Session;

namespace Neba.Website.Tests.Account.Session;

[UnitTest]
[Component("Website.Account.Session")]
public sealed class SessionCookieSyncTests
{
    private static readonly SessionTokens Tokens = new("user-123", "admin@bowlneba.com", "access-token", "refresh-token");

    private readonly SessionTokenProtector _protector = new(new EphemeralDataProtectionProvider(), NullLogger<SessionTokenProtector>.Instance);

    [Fact(DisplayName = "PersistAsync should send a protected blob, never the raw tokens, to the browser")]
    public async Task PersistAsync_ShouldSendProtectedBlob()
    {
        // Arrange
        string? sentBlob = null;
        var (jsRuntime, module) = CreateJs();
        module
            .Setup(m => m.InvokeAsync<bool>("persist", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
            .Callback<string, CancellationToken, object?[]?>((_, _, args) => sentBlob = (string?)args![0])
            .ReturnsAsync(true);
        await using var sut = new SessionCookieSync(jsRuntime.Object, _protector, TimeProvider.System, NullLogger<SessionCookieSync>.Instance);

        // Act
        var result = await sut.PersistAsync(Tokens, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeTrue();
        sentBlob.ShouldNotBeNull();
        sentBlob.ShouldNotContain("refresh-token");
        _protector.Unprotect(sentBlob).ShouldBe(Tokens);
    }

    [Fact(DisplayName = "PersistAsync should return false when the circuit has disconnected")]
    public async Task PersistAsync_ShouldReturnFalse_WhenJsDisconnected()
    {
        // Arrange
        var (jsRuntime, module) = CreateJs();
        module
            .Setup(m => m.InvokeAsync<bool>("persist", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
            .ThrowsAsync(new JSDisconnectedException("gone"));
        await using var sut = new SessionCookieSync(jsRuntime.Object, _protector, TimeProvider.System, NullLogger<SessionCookieSync>.Instance);

        // Act
        var result = await sut.PersistAsync(Tokens, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "ReadAsync should return the tokens inside the blob the browser returns")]
    public async Task ReadAsync_ShouldReturnTokens_WhenBrowserReturnsBlob()
    {
        // Arrange
        var (jsRuntime, module) = CreateJs();
        module
            .Setup(m => m.InvokeAsync<string?>("read", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
            .ReturnsAsync(_protector.Protect(Tokens));
        await using var sut = new SessionCookieSync(jsRuntime.Object, _protector, TimeProvider.System, NullLogger<SessionCookieSync>.Instance);

        // Act
        var result = await sut.ReadAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(Tokens);
    }

    [Fact(DisplayName = "ReadAsync should return null when the browser has no session")]
    public async Task ReadAsync_ShouldReturnNull_WhenBrowserReturnsNothing()
    {
        // Arrange
        var (jsRuntime, module) = CreateJs();
        module
            .Setup(m => m.InvokeAsync<string?>("read", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
            .ReturnsAsync((string?)null);
        await using var sut = new SessionCookieSync(jsRuntime.Object, _protector, TimeProvider.System, NullLogger<SessionCookieSync>.Instance);

        // Act
        var result = await sut.ReadAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "ReadAsync should return null when the JS call fails")]
    public async Task ReadAsync_ShouldReturnNull_WhenJsThrows()
    {
        // Arrange
        var (jsRuntime, module) = CreateJs();
        module
            .Setup(m => m.InvokeAsync<string?>("read", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
            .ThrowsAsync(new JSException("fetch failed"));
        await using var sut = new SessionCookieSync(jsRuntime.Object, _protector, TimeProvider.System, NullLogger<SessionCookieSync>.Instance);

        // Act
        var result = await sut.ReadAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Should import the JS module once and reuse it across calls")]
    public async Task ShouldImportModuleOnce()
    {
        // Arrange
        var (jsRuntime, module) = CreateJs();
        module
            .Setup(m => m.InvokeAsync<string?>("read", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
            .ReturnsAsync((string?)null);
        await using var sut = new SessionCookieSync(jsRuntime.Object, _protector, TimeProvider.System, NullLogger<SessionCookieSync>.Instance);
        var ct = TestContext.Current.CancellationToken;

        // Act
        await sut.ReadAsync(ct);
        await sut.ReadAsync(ct);

        // Assert
        jsRuntime.Verify(
            j => j.InvokeAsync<IJSObjectReference>("import", It.IsAny<CancellationToken>(), It.Is<object?[]?>(a => (string?)a![0] == "./js/session-tokens.js")),
            Times.Once);
    }

    [Fact(DisplayName = "PersistAsync should give up and return false when the browser doesn't answer in time")]
    public async Task PersistAsync_ShouldReturnFalse_WhenBrowserStalls()
    {
        // Arrange
        var (jsRuntime, module) = CreateJs();
        module
            .Setup(m => m.InvokeAsync<bool>("persist", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
            .Returns((string _, CancellationToken ct, object?[]? _) => HangAsync<bool>(ct));
        var time = new FakeTimeProvider();
        await using var sut = new SessionCookieSync(jsRuntime.Object, _protector, time, NullLogger<SessionCookieSync>.Instance);

        // Act
        var pending = sut.PersistAsync(Tokens, TestContext.Current.CancellationToken);
        time.Advance(SessionCookieSync.InteropTimeout + TimeSpan.FromSeconds(1));
        var result = await pending;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "ReadAsync should give up and return null when the browser doesn't answer in time")]
    public async Task ReadAsync_ShouldReturnNull_WhenBrowserStalls()
    {
        // Arrange
        var (jsRuntime, module) = CreateJs();
        module
            .Setup(m => m.InvokeAsync<string?>("read", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
            .Returns((string _, CancellationToken ct, object?[]? _) => HangAsync<string?>(ct));
        var time = new FakeTimeProvider();
        await using var sut = new SessionCookieSync(jsRuntime.Object, _protector, time, NullLogger<SessionCookieSync>.Instance);

        // Act
        var pending = sut.ReadAsync(TestContext.Current.CancellationToken);
        time.Advance(SessionCookieSync.InteropTimeout + TimeSpan.FromSeconds(1));
        var result = await pending;

        // Assert
        result.ShouldBeNull();
    }

    private static async ValueTask<T> HangAsync<T>(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return default!;
    }

    private static (Mock<IJSRuntime> JsRuntime, Mock<IJSObjectReference> Module) CreateJs()
    {
        var module = new Mock<IJSObjectReference>(MockBehavior.Strict);
        module.Setup(m => m.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
        jsRuntime
            .Setup(j => j.InvokeAsync<IJSObjectReference>("import", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
            .ReturnsAsync(module.Object);

        return (jsRuntime, module);
    }
}