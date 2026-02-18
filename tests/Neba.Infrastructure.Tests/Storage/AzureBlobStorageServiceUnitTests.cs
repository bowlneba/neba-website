using Azure.Storage.Blobs;

using Microsoft.Extensions.Logging.Abstractions;

using Neba.Application.Clock;
using Neba.Infrastructure.Storage;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Storage;

[UnitTest]
[Component("Storage")]
public sealed class AzureBlobStorageServiceUnitTests
{
    [Fact(DisplayName = "Constructor should throw when blobServiceClient is null")]
    public void Constructor_ThrowsArgumentNullException_WhenBlobServiceClientIsNull()
    {
        // Arrange
        BlobServiceClient? nullClient = null;
        var stopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict).Object;
        var logger = NullLogger<AzureBlobStorageService>.Instance;

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            new AzureBlobStorageService(nullClient!, stopwatch, logger));

        exception.ParamName.ShouldBe("blobServiceClient");
    }

    [Fact(DisplayName = "Constructor should throw when stopwatchProvider is null")]
    public void Constructor_ThrowsArgumentNullException_WhenStopwatchProviderIsNull()
    {
        // Arrange
        var client = new Mock<BlobServiceClient>().Object;
        IStopwatchProvider? nullStopwatch = null;
        var logger = NullLogger<AzureBlobStorageService>.Instance;

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            new AzureBlobStorageService(client, nullStopwatch!, logger));

        exception.ParamName.ShouldBe("stopwatchProvider");
    }

    [Fact(DisplayName = "Constructor should throw when logger is null")]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Arrange
        var client = new Mock<BlobServiceClient>().Object;
        var stopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict).Object;
        Microsoft.Extensions.Logging.ILogger<AzureBlobStorageService>? nullLogger = null;

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            new AzureBlobStorageService(client, stopwatch, nullLogger!));

        exception.ParamName.ShouldBe("logger");
    }

    private static AzureBlobStorageService CreateSutWithThrowingClient(Exception exceptionToThrow)
    {
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Throws(exceptionToThrow);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.FromMilliseconds(10));

        return new AzureBlobStorageService(
            mockBlobServiceClient.Object,
            mockStopwatch.Object,
            NullLogger<AzureBlobStorageService>.Instance);
    }

    [Fact(DisplayName = "ExistsAsync should rethrow when storage client throws")]
    public async Task ExistsAsync_ShouldRethrow_WhenStorageClientThrows()
    {
        // Arrange
        var sut = CreateSutWithThrowingClient(new InvalidOperationException("Storage failure"));

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => sut.ExistsAsync("container", "path.txt", CancellationToken.None));

        exception.Message.ShouldBe("Storage failure");
    }

    [Fact(DisplayName = "GetFileAsync should rethrow when storage client throws")]
    public async Task GetFileAsync_ShouldRethrow_WhenStorageClientThrows()
    {
        // Arrange
        var sut = CreateSutWithThrowingClient(new InvalidOperationException("Storage failure"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.GetFileAsync("container", "path.txt", CancellationToken.None));
    }

    [Fact(DisplayName = "UploadFileAsync should rethrow when storage client throws")]
    public async Task UploadFileAsync_ShouldRethrow_WhenStorageClientThrows()
    {
        // Arrange
        var sut = CreateSutWithThrowingClient(new InvalidOperationException("Storage failure"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.UploadFileAsync(
                "container", "path.txt", "content", "text/plain",
                new Dictionary<string, string>(), CancellationToken.None));
    }
}