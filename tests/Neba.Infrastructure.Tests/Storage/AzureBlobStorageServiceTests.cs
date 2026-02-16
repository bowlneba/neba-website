using Neba.Infrastructure.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Storage;

namespace Neba.Infrastructure.Tests.Storage;

[IntegrationTest]
[Component("Storage")]
[Collection<AzuriteFixture>]
public sealed class AzureBlobStorageServiceTests : IClassFixture<AzuriteFixture>
{
    private readonly AzureBlobStorageService _sut;

    public AzureBlobStorageServiceTests(AzuriteFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _sut = new AzureBlobStorageService(fixture.BlobServiceClient);
    }

    private static string UniqueContainer() => $"test-{Guid.NewGuid():N}";

    [Fact(DisplayName = "ExistsAsync should return false when file does not exist")]
    public async Task ExistsAsync_ReturnsFalse_WhenFileDoesNotExist()
    {
        // Arrange
        var container = UniqueContainer();

        // Act
        var result = await _sut.ExistsAsync(container, "nonexistent.txt", CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "ExistsAsync should return true after file is uploaded")]
    public async Task ExistsAsync_ReturnsTrue_AfterFileIsUploaded()
    {
        // Arrange
        var container = UniqueContainer();
        const string path = "test-file.txt";

        await _sut.UploadFileAsync(
            container,
            path,
            StoredFileFactory.ValidContent,
            StoredFileFactory.ValidContentType,
            new Dictionary<string, string>(StoredFileFactory.ValidMetadata),
            CancellationToken.None);

        // Act
        var result = await _sut.ExistsAsync(container, path, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "GetFileAsync should return null when file does not exist")]
    public async Task GetFileAsync_ReturnsNull_WhenFileDoesNotExist()
    {
        // Arrange
        var container = UniqueContainer();

        // Act
        var result = await _sut.GetFileAsync(container, "nonexistent.txt", CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "GetFileAsync should return content and metadata after upload")]
    public async Task GetFileAsync_ReturnsStoredFile_AfterUpload()
    {
        // Arrange
        var container = UniqueContainer();
        const string path = "test-file.html";
        const string content = "<h1>Test</h1>";
        const string contentType = "text/html";
        var metadata = new Dictionary<string, string> { ["Source"] = "GoogleDrive" };

        await _sut.UploadFileAsync(container, path, content, contentType, metadata, CancellationToken.None);

        // Act
        var result = await _sut.GetFileAsync(container, path, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Content.ShouldBe(content);
        result.ContentType.ShouldBe(contentType);
        result.Metadata.ShouldContainKeyAndValue("Source", "GoogleDrive");
    }

    [Fact(DisplayName = "UploadFileAsync should create container if it does not exist")]
    public async Task UploadFileAsync_CreatesContainer_WhenItDoesNotExist()
    {
        // Arrange
        var container = UniqueContainer();

        // Act & Assert — should not throw
        await Should.NotThrowAsync(() => _sut.UploadFileAsync(
            container,
            "new-file.txt",
            StoredFileFactory.ValidContent,
            StoredFileFactory.ValidContentType,
            new Dictionary<string, string>(StoredFileFactory.ValidMetadata),
            CancellationToken.None));
    }

    [Fact(DisplayName = "UploadFileAsync should overwrite existing file")]
    public async Task UploadFileAsync_OverwritesExistingFile()
    {
        // Arrange
        var container = UniqueContainer();
        const string path = "overwrite-me.txt";

        await _sut.UploadFileAsync(
            container, path, "original", "text/plain",
            new Dictionary<string, string>(), CancellationToken.None);

        // Act
        await _sut.UploadFileAsync(
            container, path, "updated", "text/plain",
            new Dictionary<string, string>(), CancellationToken.None);

        // Assert
        var result = await _sut.GetFileAsync(container, path, CancellationToken.None);
        result.ShouldNotBeNull();
        result.Content.ShouldBe("updated");
    }

    [Fact(DisplayName = "GetFileAsync should preserve multiple metadata entries")]
    public async Task GetFileAsync_PreservesMultipleMetadataEntries()
    {
        // Arrange
        var container = UniqueContainer();
        const string path = "metadata-test.txt";
        var metadata = new Dictionary<string, string>
        {
            ["Author"] = "TestUser",
            ["LastUpdatedUtc"] = "2026-02-16T00:00:00Z",
            ["Source"] = "GoogleDrive"
        };

        await _sut.UploadFileAsync(
            container, path, StoredFileFactory.ValidContent,
            StoredFileFactory.ValidContentType, metadata, CancellationToken.None);

        // Act
        var result = await _sut.GetFileAsync(container, path, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Metadata.Count.ShouldBe(3);
        result.Metadata.ShouldContainKeyAndValue("Author", "TestUser");
        result.Metadata.ShouldContainKeyAndValue("LastUpdatedUtc", "2026-02-16T00:00:00Z");
        result.Metadata.ShouldContainKeyAndValue("Source", "GoogleDrive");
    }
}
