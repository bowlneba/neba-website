using ErrorOr;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Documents;

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

        var handler = new RefreshDocumentCommandHandler(fusionCache, storageServiceMock.Object, CreateGoogleSettings("bylaws", "tournament-rules"), new FakeLogger<RefreshDocumentCommandHandler>());

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

        var handler = new RefreshDocumentCommandHandler(fusionCache, storageServiceMock.Object, CreateGoogleSettings("bylaws", "tournament-rules"), new FakeLogger<RefreshDocumentCommandHandler>());

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
            .Setup(s => s.DeleteAsync("bowlneba-private", "documents/bylaws", ct))
            .ReturnsAsync(false);

        var handler = new RefreshDocumentCommandHandler(fusionCache, storageServiceMock.Object, CreateGoogleSettings("bylaws", "tournament-rules"), new FakeLogger<RefreshDocumentCommandHandler>());

        // Act
        var result = await handler.HandleAsync(new RefreshDocumentCommand { DocumentName = "bylaws" }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync returns Deleted, logs a warning, and touches nothing when the document isn't listed in appsettings")]
    public async Task HandleAsync_ShouldReturnDeletedAndLogWarning_WhenDocumentIsNotListed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fusionCache = _serviceProvider.GetRequiredService<IFusionCache>();

        // Strict with no setups: any storage call throws.
        var storageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var logger = new FakeLogger<RefreshDocumentCommandHandler>();

        await fusionCache.SetAsync("probe:secret", "value", tags: ["neba:document:secret"], token: ct);

        var handler = new RefreshDocumentCommandHandler(fusionCache, storageServiceMock.Object, CreateGoogleSettings("bylaws", "tournament-rules"), logger);

        // Act
        var result = await handler.HandleAsync(new RefreshDocumentCommand { DocumentName = "secret" }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(Result.Deleted);
        (await fusionCache.TryGetAsync<string>("probe:secret", token: ct)).HasValue.ShouldBeTrue();

        var record = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("secret");
        record.Message.ShouldContain("Google:Documents");
    }

    [Fact(DisplayName = "HandleAsync treats document names as case-sensitive when checking the appsettings list")]
    public async Task HandleAsync_ShouldLogWarning_WhenDocumentNameDiffersOnlyByCase()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fusionCache = _serviceProvider.GetRequiredService<IFusionCache>();
        var storageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var logger = new FakeLogger<RefreshDocumentCommandHandler>();

        var handler = new RefreshDocumentCommandHandler(fusionCache, storageServiceMock.Object, CreateGoogleSettings("bylaws"), logger);

        // Act
        var result = await handler.HandleAsync(new RefreshDocumentCommand { DocumentName = "Bylaws" }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }

    [Fact(DisplayName = "HandleAsync logs nothing when the document is listed in appsettings")]
    public async Task HandleAsync_ShouldNotLog_WhenDocumentIsListed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var fusionCache = _serviceProvider.GetRequiredService<IFusionCache>();
        var storageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        storageServiceMock
            .Setup(s => s.DeleteAsync("bowlneba-private", "documents/bylaws", ct))
            .ReturnsAsync(true);
        var logger = new FakeLogger<RefreshDocumentCommandHandler>();

        var handler = new RefreshDocumentCommandHandler(fusionCache, storageServiceMock.Object, CreateGoogleSettings("bylaws"), logger);

        // Act
        await handler.HandleAsync(new RefreshDocumentCommand { DocumentName = "bylaws" }, ct);

        // Assert
        logger.Collector.GetSnapshot().ShouldBeEmpty();
    }
}
