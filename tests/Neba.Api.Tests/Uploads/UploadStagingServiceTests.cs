using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Database;
using Neba.Api.Storage;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Uploads;

[IntegrationTest]
[Component("Uploads")]
[Collection<AppDbContextFixture>]
public sealed class UploadStagingServiceTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private readonly Mock<IFileStorageService> _fileStorageServiceMock = new(MockBehavior.Strict);
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private UploadStagingService CreateSut()
        => new(_fileStorageServiceMock.Object, _dbContext, _timeProvider);

    private static Mock<IFormFile> CreateFormFile(
        string fileName = "photo.png",
        string contentType = "image/png",
        long length = 1024,
        Stream? content = null)
    {
        var formFile = new Mock<IFormFile>(MockBehavior.Strict);
        formFile.SetupGet(f => f.FileName).Returns(fileName);
        formFile.SetupGet(f => f.ContentType).Returns(contentType);
        formFile.SetupGet(f => f.Length).Returns(length);
        formFile.Setup(f => f.OpenReadStream()).Returns(content ?? new MemoryStream());

        return formFile;
    }

    [Fact(DisplayName = "StageUploadAsync should upload the file's stream to the given container and a generated path")]
    public async Task StageUploadAsync_ShouldUploadFileStream_ToContainerAndGeneratedPath()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var formFile = CreateFormFile(fileName: "cover.png", contentType: "image/png");
        const string container = "bowlneba-private";
        const string pathPrefix = "articles/1";

        Stream? uploadedStream = null;
        string? uploadedPath = null;

        _fileStorageServiceMock
            .Setup(s => s.UploadFileAsync(
                container,
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                "image/png",
                It.IsAny<IDictionary<string, string>>(),
                ct))
            .Callback<string, string, Stream, string, IDictionary<string, string>, CancellationToken>(
                (_, path, stream, _, _, _) =>
                {
                    uploadedPath = path;
                    uploadedStream = stream;
                })
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        await sut.StageUploadAsync(formFile.Object, container, pathPrefix, null, ct);

        // Assert
        _fileStorageServiceMock.VerifyAll();
        uploadedStream.ShouldNotBeNull();
        uploadedPath.ShouldNotBeNull();
        uploadedPath.ShouldStartWith("uploads/articles/1/");
        uploadedPath.ShouldEndWith("-cover.png");
    }

    [Theory(DisplayName = "StageUploadAsync should strip directory components from the file name before building the path")]
    [InlineData("../../etc/evil.png", "evil.png", TestDisplayName = "Unix-style path traversal")]
    [InlineData("..\\..\\evil.png", "evil.png", TestDisplayName = "Windows-style path traversal")]
    [InlineData("sub/dir/photo.png", "photo.png", TestDisplayName = "Nested forward-slash path")]
    public async Task StageUploadAsync_ShouldStripDirectoryComponents_FromFileName(string suppliedFileName, string expectedSuffix)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var formFile = CreateFormFile(fileName: suppliedFileName);
        const string container = "bowlneba-private";

        _fileStorageServiceMock
            .Setup(s => s.UploadFileAsync(
                container,
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                ct))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.StageUploadAsync(formFile.Object, container, "articles/traversal", null, ct);

        // Assert
        _fileStorageServiceMock.VerifyAll();
        result.Path.ShouldStartWith("uploads/articles/traversal/");
        result.Path.ShouldEndWith("-" + expectedSuffix);
        result.Path.ShouldNotContain("..");
    }

    [Fact(DisplayName = "StageUploadAsync should pass an empty metadata dictionary when metadata is null")]
    public async Task StageUploadAsync_ShouldPassEmptyMetadata_WhenMetadataIsNull()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var formFile = CreateFormFile();

        _fileStorageServiceMock
            .Setup(s => s.UploadFileAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.Is<IDictionary<string, string>>(m => m.Count == 0),
                ct))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        await sut.StageUploadAsync(formFile.Object, "bowlneba-private", "articles/1", null, ct);

        // Assert
        _fileStorageServiceMock.VerifyAll();
    }

    [Fact(DisplayName = "StageUploadAsync should pass the provided metadata through to the file storage service")]
    public async Task StageUploadAsync_ShouldPassProvidedMetadata_WhenMetadataIsSupplied()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var formFile = CreateFormFile();
        var metadata = new Dictionary<string, string> { ["source_document_id"] = "abc123" };

        _fileStorageServiceMock
            .Setup(s => s.UploadFileAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.Is<IDictionary<string, string>>(m => m.Count == 1 && m["source_document_id"] == "abc123"),
                ct))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        await sut.StageUploadAsync(formFile.Object, "bowlneba-private", "articles/1", metadata, ct);

        // Assert
        _fileStorageServiceMock.VerifyAll();
    }

    [Fact(DisplayName = "StageUploadAsync should persist a pending upload record with the container, generated path, and current time")]
    public async Task StageUploadAsync_ShouldPersistPendingUpload_WithContainerPathAndCurrentTime()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var formFile = CreateFormFile(fileName: "persist-me.png");
        const string container = "bowlneba-private";

        _fileStorageServiceMock
            .Setup(s => s.UploadFileAsync(
                container,
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                ct))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.StageUploadAsync(formFile.Object, container, "articles/2", null, ct);

        // Assert
        _fileStorageServiceMock.VerifyAll();

        var pendingUpload = await _dbContext.PendingUploads
            .AsNoTracking()
            .SingleAsync(u => u.Container == container && u.Path == result.Path, ct);

        pendingUpload.Container.ShouldBe(container);
        pendingUpload.Path.ShouldBe(result.Path);
        pendingUpload.UploadedAtUtc.ShouldBe(_timeProvider.GetUtcNow());
    }

    [Fact(DisplayName = "StageUploadAsync should return a stored file describing the container, generated path, content type, and size")]
    public async Task StageUploadAsync_ShouldReturnStoredFile_DescribingContainerPathContentTypeAndSize()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var formFile = CreateFormFile(fileName: "returned.png", contentType: "image/png", length: 2048);
        const string container = "bowlneba-private";

        _fileStorageServiceMock
            .Setup(s => s.UploadFileAsync(
                container,
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                "image/png",
                It.IsAny<IDictionary<string, string>>(),
                ct))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.StageUploadAsync(formFile.Object, container, "articles/3", null, ct);

        // Assert
        _fileStorageServiceMock.VerifyAll();
        result.Container.ShouldBe(container);
        result.Path.ShouldStartWith("uploads/articles/3/");
        result.Path.ShouldEndWith("-returned.png");
        result.ContentType.ShouldBe("image/png");
        result.SizeInBytes.ShouldBe(2048);
    }

    [Fact(DisplayName = "StageUploadAsync should stage the given stream with its own content type and length")]
    public async Task StageUploadAsync_ShouldStageGivenStream_WithItsContentTypeAndLength()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var content = new MemoryStream([1, 2, 3, 4, 5]);
        string? uploadedPath = null;

        _fileStorageServiceMock
            .Setup(s => s.UploadFileAsync(
                "bowlneba-public",
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                "image/jpeg",
                It.IsAny<IDictionary<string, string>>(),
                ct))
            .Callback<string, string, Stream, string, IDictionary<string, string>, CancellationToken>(
                (_, path, _, _, _, _) => uploadedPath = path)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.StageUploadAsync(content, "../photo.jpg", "image/jpeg", "bowlneba-public", "hall-of-fame/photo", null, ct);

        // Assert
        result.ContentType.ShouldBe("image/jpeg");
        result.SizeInBytes.ShouldBe(5);
        result.Path.ShouldBe(uploadedPath);
        result.Path.ShouldStartWith("uploads/hall-of-fame/photo/");
        result.Path.ShouldEndWith("-photo.jpg");
        (await _dbContext.PendingUploads.AnyAsync(p => p.Path == result.Path, ct)).ShouldBeTrue();
    }

    [Fact(DisplayName = "StageUploadAsync should report only the bytes from the stream's current position")]
    public async Task StageUploadAsync_ShouldReportRemainingBytes_WhenStreamIsPartlyRead()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var content = new MemoryStream([1, 2, 3, 4, 5]) { Position = 2 };

        _fileStorageServiceMock
            .Setup(s => s.UploadFileAsync(
                "bowlneba-public",
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                "image/jpeg",
                It.IsAny<IDictionary<string, string>>(),
                ct))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.StageUploadAsync(content, "photo.jpg", "image/jpeg", "bowlneba-public", "hall-of-fame/photo", null, ct);

        // Assert
        result.SizeInBytes.ShouldBe(3);
    }

    [Fact(DisplayName = "StageUploadAsync should throw when the given stream is not seekable")]
    public async Task StageUploadAsync_ShouldThrow_WhenStreamIsNotSeekable()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var content = new Mock<Stream>(MockBehavior.Strict);
        content.SetupGet(s => s.CanSeek).Returns(false);

        var sut = CreateSut();

        // Act
        var act = () => sut.StageUploadAsync(content.Object, "photo.jpg", "image/jpeg", "bowlneba-public", "hall-of-fame/photo", null, ct);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }
}
