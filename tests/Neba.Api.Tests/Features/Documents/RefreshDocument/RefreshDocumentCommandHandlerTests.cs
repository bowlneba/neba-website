using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Features.Documents.RefreshDocument;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.Documents.RefreshDocument;

[UnitTest]
[Component("Documents")]
public sealed class RefreshDocumentCommandHandlerTests : IAsyncLifetime
{
    private ServiceProvider _serviceProvider = null!;

    public ValueTask InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddFusionCache()
            .WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromHours(1));
        _serviceProvider = services.BuildServiceProvider();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
        => await _serviceProvider.DisposeAsync();

    [Fact(DisplayName = "HandleAsync evicts only the target document's FusionCache tag")]
    public async Task HandleAsync_ShouldEvictOnlyTargetDocumentsFusionCacheTag()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fusionCache = _serviceProvider.GetRequiredService<IFusionCache>();

        var storageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        storageServiceMock
            .Setup(s => s.DeleteAsync("bowlneba-private", "documents/bylaws", ct))
            .ReturnsAsync(true);

        await fusionCache.SetAsync("probe:bylaws", "value", tags: ["neba:document:bylaws"], token: ct);
        await fusionCache.SetAsync("probe:tournament-rules", "value", tags: ["neba:document:tournament-rules"], token: ct);

        var handler = new RefreshDocumentCommandHandler(fusionCache, storageServiceMock.Object);

        // Act
        await handler.HandleAsync(new RefreshDocumentCommand { DocumentName = "bylaws" }, ct);

        // Assert
        (await fusionCache.TryGetAsync<string>("probe:bylaws", token: ct)).HasValue.ShouldBeFalse();
        (await fusionCache.TryGetAsync<string>("probe:tournament-rules", token: ct)).HasValue.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync deletes the target document's blob from storage")]
    public async Task HandleAsync_ShouldDeleteTargetDocumentsBlob_FromStorage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fusionCache = _serviceProvider.GetRequiredService<IFusionCache>();

        var storageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        storageServiceMock
            .Setup(s => s.DeleteAsync("bowlneba-private", "documents/tournament-rules", ct))
            .ReturnsAsync(true)
            .Verifiable();

        var handler = new RefreshDocumentCommandHandler(fusionCache, storageServiceMock.Object);

        // Act
        await handler.HandleAsync(new RefreshDocumentCommand { DocumentName = "tournament-rules" }, ct);

        // Assert
        storageServiceMock.VerifyAll();
    }

    [Fact(DisplayName = "HandleAsync returns Deleted even when nothing was cached")]
    public async Task HandleAsync_ShouldReturnDeleted_WhenNothingWasCached()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fusionCache = _serviceProvider.GetRequiredService<IFusionCache>();

        var storageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        storageServiceMock
            .Setup(s => s.DeleteAsync("bowlneba-private", "documents/unknown-document", ct))
            .ReturnsAsync(false);

        var handler = new RefreshDocumentCommandHandler(fusionCache, storageServiceMock.Object);

        // Act
        var result = await handler.HandleAsync(new RefreshDocumentCommand { DocumentName = "unknown-document" }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }
}