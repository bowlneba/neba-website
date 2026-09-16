using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Documents;
using Neba.Api.Features.Cache.ClearCache;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.Cache.ClearCache;

[UnitTest]
[Component("Cache")]
public sealed class ClearCacheCommandHandlerTests : IAsyncLifetime
{
    private ServiceProvider _serviceProvider = null!;

    public ValueTask InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddFusionCache()
            .WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromHours(1));
        services.AddHybridCache();
        _serviceProvider = services.BuildServiceProvider();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
        => await _serviceProvider.DisposeAsync();

    private static GoogleSettings CreateGoogleSettings(params string[] documentNames)
        => new()
        {
            ApplicationName = "Test Application",
            Credentials = new GoogleCredentials
            {
                ProjectId = "test-project",
                PrivateKey = "test-key",
                ClientEmail = "test@test.iam.gserviceaccount.com",
                PrivateKeyId = "test-key-id"
            },
            Documents = [.. documentNames.Select(name => new GoogleDocument { DocumentId = $"{name}-id", Name = name, WebRoute = $"/{name}" })]
        };

    [Fact(DisplayName = "HandleAsync evicts the neba-tagged FusionCache entries")]
    public async Task HandleAsync_ShouldEvictNebaTaggedFusionCacheEntries()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fusionCache = _serviceProvider.GetRequiredService<IFusionCache>();
        var hybridCache = _serviceProvider.GetRequiredService<HybridCache>();
        var storageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var googleSettings = CreateGoogleSettings();

        await fusionCache.SetAsync("probe:fusion", "value", tags: ["neba"], token: ct);

        var handler = new ClearCacheCommandHandler(fusionCache, hybridCache, storageServiceMock.Object, googleSettings);

        // Act
        await handler.HandleAsync(new ClearCacheCommand(), ct);

        // Assert
        (await fusionCache.TryGetAsync<string>("probe:fusion", token: ct)).HasValue.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync evicts the neba-tagged HybridCache entries")]
    public async Task HandleAsync_ShouldEvictNebaTaggedHybridCacheEntries()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fusionCache = _serviceProvider.GetRequiredService<IFusionCache>();
        var hybridCache = _serviceProvider.GetRequiredService<HybridCache>();
        var storageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var googleSettings = CreateGoogleSettings();

        await hybridCache.SetAsync("probe:hybrid", "value", tags: ["neba"], cancellationToken: ct);

        var handler = new ClearCacheCommandHandler(fusionCache, hybridCache, storageServiceMock.Object, googleSettings);

        // Act
        await handler.HandleAsync(new ClearCacheCommand(), ct);

        // Assert — a stale cached value would be returned instead of invoking the factory
        var afterClear = await hybridCache.GetOrCreateAsync(
            "probe:hybrid",
            _ => ValueTask.FromResult("fresh-value"),
            cancellationToken: ct);
        afterClear.ShouldBe("fresh-value");
    }

    [Fact(DisplayName = "HandleAsync deletes every configured Google document from blob storage")]
    public async Task HandleAsync_ShouldDeleteEveryConfiguredGoogleDocument_FromBlobStorage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fusionCache = _serviceProvider.GetRequiredService<IFusionCache>();
        var hybridCache = _serviceProvider.GetRequiredService<HybridCache>();
        var googleSettings = CreateGoogleSettings("bylaws", "membership-handbook");

        var storageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        storageServiceMock
            .Setup(s => s.DeleteAsync("bowlneba-private", "documents/bylaws", ct))
            .ReturnsAsync(true);
        storageServiceMock
            .Setup(s => s.DeleteAsync("bowlneba-private", "documents/membership-handbook", ct))
            .ReturnsAsync(true);

        var handler = new ClearCacheCommandHandler(fusionCache, hybridCache, storageServiceMock.Object, googleSettings);

        // Act
        var result = await handler.HandleAsync(new ClearCacheCommand(), ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync returns Success when there are no configured Google documents")]
    public async Task HandleAsync_ShouldReturnSuccess_WhenNoGoogleDocumentsAreConfigured()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fusionCache = _serviceProvider.GetRequiredService<IFusionCache>();
        var hybridCache = _serviceProvider.GetRequiredService<HybridCache>();
        var storageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var googleSettings = CreateGoogleSettings();

        var handler = new ClearCacheCommandHandler(fusionCache, hybridCache, storageServiceMock.Object, googleSettings);

        // Act
        var result = await handler.HandleAsync(new ClearCacheCommand(), ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }
}