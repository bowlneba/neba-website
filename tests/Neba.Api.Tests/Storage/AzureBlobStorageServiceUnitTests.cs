using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Clock;

using Neba.Api.Storage;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Storage;

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

    [Fact(DisplayName = "UploadFileAsync (stream) should rethrow when storage client throws")]
    public async Task UploadFileAsync_Stream_ShouldRethrow_WhenStorageClientThrows()
    {
        // Arrange
        var sut = CreateSutWithThrowingClient(new InvalidOperationException("Storage failure"));
        await using var stream = new MemoryStream("content"u8.ToArray());

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.UploadFileAsync(
                "container", "path.txt", stream, "text/plain",
                new Dictionary<string, string>(), CancellationToken.None));
    }

    private static Mock<BlobContainerClient> CreateStrictContainerClientMock(bool containerExists)
    {
        var mockContainerClient = new Mock<BlobContainerClient>(MockBehavior.Strict);

        mockContainerClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(containerExists, Mock.Of<Response>()));

        mockContainerClient
            .Setup(x => x.SetAccessPolicyAsync(
                It.IsAny<PublicAccessType>(),
                It.IsAny<IEnumerable<BlobSignedIdentifier>>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

        var mockBlobClient = new Mock<BlobClient>(MockBehavior.Strict);
        mockBlobClient
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<BlobUploadOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        mockContainerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(mockBlobClient.Object);

        return mockContainerClient;
    }

    private static AzureBlobStorageService CreateSutWithContainerClient(Mock<BlobContainerClient> mockContainerClient)
    {
        var mockBlobServiceClient = new Mock<BlobServiceClient>(MockBehavior.Strict);
        mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(mockContainerClient.Object);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.FromMilliseconds(10));

        return new AzureBlobStorageService(
            mockBlobServiceClient.Object,
            mockStopwatch.Object,
            NullLogger<AzureBlobStorageService>.Instance);
    }

    [Fact(DisplayName = "UploadFileAsync should not create container when container already exists")]
    public async Task UploadFileAsync_ShouldNotCreateContainer_WhenContainerAlreadyExists()
    {
        // Arrange
        Mock<BlobContainerClient> mockContainerClient = CreateStrictContainerClientMock(containerExists: true);
        AzureBlobStorageService sut = CreateSutWithContainerClient(mockContainerClient);

        // Act & Assert - the Strict mock has no Setup for CreateIfNotExistsAsync, so it
        // would throw if UploadFileAsync called it
        await Should.NotThrowAsync(() => sut.UploadFileAsync(
            "container", "path.txt", "content", "text/plain",
            new Dictionary<string, string>(), CancellationToken.None));
    }

    [Fact(DisplayName = "UploadFileAsync should create container when container does not exist")]
    public async Task UploadFileAsync_ShouldCreateContainer_WhenContainerDoesNotExist()
    {
        // Arrange
        Mock<BlobContainerClient> mockContainerClient = CreateStrictContainerClientMock(containerExists: false);
        var containerCreated = false;
        mockContainerClient
            .Setup(x => x.CreateIfNotExistsAsync(
                It.IsAny<PublicAccessType>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<BlobContainerEncryptionScopeOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => containerCreated = true)
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());
        AzureBlobStorageService sut = CreateSutWithContainerClient(mockContainerClient);

        // Act
        await sut.UploadFileAsync(
            "container", "path.txt", "content", "text/plain",
            new Dictionary<string, string>(), CancellationToken.None);

        // Assert
        containerCreated.ShouldBeTrue();
    }

    [Fact(DisplayName = "GetBlobUri should return URI from blob client")]
    public void GetBlobUri_ShouldReturnUri_FromBlobClient()
    {
        // Arrange
        var expectedUri = new Uri("https://storage.example.com/container/path.txt");

        var mockBlobClient = new Mock<BlobClient>(MockBehavior.Strict);
        mockBlobClient.Setup(x => x.Uri).Returns(expectedUri);

        var mockContainerClient = new Mock<BlobContainerClient>(MockBehavior.Strict);
        mockContainerClient
            .Setup(x => x.GetBlobClient("path.txt"))
            .Returns(mockBlobClient.Object);

        var mockBlobServiceClient = new Mock<BlobServiceClient>(MockBehavior.Strict);
        mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient("container"))
            .Returns(mockContainerClient.Object);

        var sut = new AzureBlobStorageService(
            mockBlobServiceClient.Object,
            new Mock<IStopwatchProvider>(MockBehavior.Strict).Object,
            NullLogger<AzureBlobStorageService>.Instance);

        // Act
        var result = sut.GetBlobUri("container", "path.txt");

        // Assert
        result.ShouldBe(expectedUri);
    }

    [Fact(DisplayName = "GetBlobUri should rethrow when storage client throws")]
    public void GetBlobUri_ShouldRethrow_WhenStorageClientThrows()
    {
        // Arrange
        var sut = CreateSutWithThrowingClient(new InvalidOperationException("Storage failure"));

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(
            () => sut.GetBlobUri("container", "path.txt"));

        exception.Message.ShouldBe("Storage failure");
    }
}