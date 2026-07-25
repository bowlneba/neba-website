using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.JSInterop;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Time;

namespace Neba.Website.Tests.Time;

[UnitTest]
[Component("Website.Time")]
public sealed class ClientTimeZoneServiceTests : IAsyncDisposable
{
    private const string EasternTimeZoneId = "America/New_York";

    private readonly Mock<IJSRuntime> _jsRuntimeMock;
    private readonly Mock<IJSObjectReference> _moduleMock;
    private readonly FakeLogger<ClientTimeZoneService> _logger;
    private readonly ClientTimeZoneService _service;

    public ClientTimeZoneServiceTests()
    {
        _jsRuntimeMock = new Mock<IJSRuntime>(MockBehavior.Strict);
        _moduleMock = new Mock<IJSObjectReference>(MockBehavior.Strict);
        _logger = new FakeLogger<ClientTimeZoneService>();
        _service = new ClientTimeZoneService(_jsRuntimeMock.Object, _logger);
    }

    public async ValueTask DisposeAsync()
    {
        _moduleMock.Setup(m => m.DisposeAsync()).Returns(ValueTask.CompletedTask);
        await _service.DisposeAsync();
    }

    private void SetupModuleImport() =>
        _jsRuntimeMock
            .Setup(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
            .Returns(new ValueTask<IJSObjectReference>(_moduleMock.Object));

    private void SetupResolvedTimeZone(string timeZoneId)
    {
        SetupModuleImport();
        _moduleMock
            .Setup(m => m.InvokeAsync<string>("getTimeZoneId", It.IsAny<object[]>()))
            .Returns(new ValueTask<string>(timeZoneId));
    }

    [Fact(DisplayName = "ToLocalAsync converts a UTC instant using the browser's resolved time zone")]
    public async Task ToLocalAsync_ShouldConvertUsingBrowserTimeZone_WhenResolutionSucceeds()
    {
        // Arrange
        SetupResolvedTimeZone(EasternTimeZoneId);
        var utc = new DateTimeOffset(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);
        var expectedZone = TimeZoneInfo.FindSystemTimeZoneById(EasternTimeZoneId);

        // Act
        var local = await _service.ToLocalAsync(utc);

        // Assert
        local.ShouldBe(TimeZoneInfo.ConvertTime(utc, expectedZone));
    }

    [Fact(DisplayName = "ToUtcAsync converts a local wall-clock value using the browser's resolved time zone")]
    public async Task ToUtcAsync_ShouldConvertUsingBrowserTimeZone_WhenResolutionSucceeds()
    {
        // Arrange
        SetupResolvedTimeZone(EasternTimeZoneId);
        var local = new DateTime(2026, 7, 25, 8, 0, 0, DateTimeKind.Unspecified);
        var expectedZone = TimeZoneInfo.FindSystemTimeZoneById(EasternTimeZoneId);
        var expectedUtc = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), expectedZone));

        // Act
        var utc = await _service.ToUtcAsync(local);

        // Assert
        utc.ShouldBe(expectedUtc);
    }

    [Fact(DisplayName = "ToLocalAsync resolves the browser time zone only once across multiple calls")]
    public async Task ToLocalAsync_ShouldResolveTimeZoneOnlyOnce_WhenCalledMultipleTimes()
    {
        // Arrange
        SetupResolvedTimeZone(EasternTimeZoneId);
        var utc = new DateTimeOffset(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);

        // Act
        await _service.ToLocalAsync(utc);
        await _service.ToLocalAsync(utc);

        // Assert
        _jsRuntimeMock.Verify(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()), Times.Once);
        _moduleMock.Verify(m => m.InvokeAsync<string>("getTimeZoneId", It.IsAny<object[]>()), Times.Once);
    }

    [Fact(DisplayName = "ToLocalAsync falls back to UTC and logs a warning when the module import throws")]
    public async Task ToLocalAsync_ShouldFallBackToUtcAndLogWarning_WhenModuleImportThrows()
    {
        // Arrange
        _jsRuntimeMock
            .Setup(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
            .Throws(new JSException("import failed"));
        var utc = new DateTimeOffset(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);

        // Act
        var local = await _service.ToLocalAsync(utc);

        // Assert
        local.ShouldBe(utc);
        _logger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Warning);
    }

    [Fact(DisplayName = "ToLocalAsync falls back to UTC and logs a warning when the resolved time zone id is invalid")]
    public async Task ToLocalAsync_ShouldFallBackToUtcAndLogWarning_WhenTimeZoneIdIsInvalid()
    {
        // Arrange
        SetupResolvedTimeZone("Not/A/Real/Zone");
        var utc = new DateTimeOffset(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);

        // Act
        var local = await _service.ToLocalAsync(utc);

        // Assert
        local.ShouldBe(utc);
        _logger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Warning);
    }

    [Fact(DisplayName = "DisposeAsync disposes the loaded JS module")]
    public async Task DisposeAsync_ShouldDisposeModule_WhenModuleWasLoaded()
    {
        // Arrange
        SetupResolvedTimeZone(EasternTimeZoneId);
        await _service.ToLocalAsync(DateTimeOffset.UtcNow);
        _moduleMock.Setup(m => m.DisposeAsync()).Returns(ValueTask.CompletedTask);

        // Act
        await _service.DisposeAsync();

        // Assert
        _moduleMock.Verify(m => m.DisposeAsync(), Times.Once);
    }

    [Fact(DisplayName = "DisposeAsync does not throw when the circuit has already disconnected")]
    public async Task DisposeAsync_ShouldNotThrow_WhenModuleDisposalThrowsJSDisconnectedException()
    {
        // Arrange
        SetupResolvedTimeZone(EasternTimeZoneId);
        await _service.ToLocalAsync(DateTimeOffset.UtcNow);
        _moduleMock.Setup(m => m.DisposeAsync()).Throws(new JSDisconnectedException("Disconnected"));

        // Act & Assert
        await Should.NotThrowAsync(async () => await _service.DisposeAsync());
    }

    [Fact(DisplayName = "DisposeAsync does nothing when no module was ever loaded")]
    public async Task DisposeAsync_ShouldDoNothing_WhenModuleWasNeverLoaded()
    {
        // Act & Assert
        await Should.NotThrowAsync(async () => await _service.DisposeAsync());
    }
}