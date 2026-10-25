using ErrorOr;

using FastEndpoints;

using Microsoft.AspNetCore.Http;

using Neba.Api.Contracts.HallOfFame.UploadHallOfFamePhoto;
using Neba.Api.Features.HallOfFame.UploadHallOfFamePhoto;
using Neba.Api.Storage;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Uploads;

namespace Neba.Api.Tests.Features.HallOfFame.UploadHallOfFamePhoto;

[UnitTest]
[Component("HallOfFame")]
public sealed class UploadHallOfFamePhotoEndpointTests
{
    private static IFormFile CreateFile(string contentType = "image/png", long length = 1024)
    {
        var stream = new MemoryStream(new byte[length]);
        return new FormFile(stream, 0, length, "File", "photo.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static Mock<IFileStorageService> CreateFileStorageServiceMock(Uri? blobUri = null)
    {
        var mock = new Mock<IFileStorageService>(MockBehavior.Strict);
        mock.Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(blobUri ?? new Uri("https://storage.example.com/test-container/photo.png"));
        return mock;
    }

    private static Mock<IImageResizer> CreateResizerMock(ErrorOr<ResizedImage> result)
    {
        var mock = new Mock<IImageResizer>(MockBehavior.Strict);
        mock.Setup(r => r.ResizeAsync(
                It.IsAny<Stream>(),
                UploadHallOfFamePhotoEndpoint.MaxPhotoDimension,
                UploadHallOfFamePhotoRequestValidator.AllowedContentTypes,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return mock;
    }

    [Fact(DisplayName = "HandleAsync should return OK with the staged file mapped to the response")]
    public async Task HandleAsync_ShouldReturnOkWithMappedFile_WhenStagingSucceeds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var storedFile = StoredFileFactory.Create();
        var blobUri = new Uri("https://storage.example.com/test-container/photo.png");

        var stagingServiceMock = new Mock<IUploadStagingService>(MockBehavior.Strict);
        stagingServiceMock
            .Setup(s => s.StageUploadAsync(It.IsAny<Stream>(), "photo.png", ResizedImageFactory.ValidContentType, "bowlneba-public", "hall-of-fame/photo", null, ct))
            .ReturnsAsync(storedFile);

        var resizerMock = CreateResizerMock(ResizedImageFactory.Create());
        var endpoint = Factory.Create<UploadHallOfFamePhotoEndpoint>(stagingServiceMock.Object, CreateFileStorageServiceMock(blobUri).Object, resizerMock.Object);

        // Act
        await endpoint.HandleAsync(new UploadHallOfFamePhotoRequest { File = CreateFile() }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Container.ShouldBe(storedFile.Container);
        endpoint.Response.Path.ShouldBe(storedFile.Path);
        endpoint.Response.FileName.ShouldBe("photo.png");
        endpoint.Response.ContentType.ShouldBe(storedFile.ContentType);
        endpoint.Response.SizeInBytes.ShouldBe(storedFile.SizeInBytes);
        endpoint.Response.Url.ShouldBe(blobUri);
    }

    [Fact(DisplayName = "HandleAsync should stage the resized bytes under the bowlneba-public container and hall-of-fame/photo prefix")]
    public async Task HandleAsync_ShouldStageResizedBytes_UnderHallOfFamePhotoPrefix()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var resized = ResizedImageFactory.Create(content: [9, 8, 7], contentType: "image/png");
        byte[]? stagedBytes = null;

        var stagingServiceMock = new Mock<IUploadStagingService>(MockBehavior.Strict);
        stagingServiceMock
            .Setup(s => s.StageUploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>?>(), ct))
            .Callback<Stream, string, string, string, string, IDictionary<string, string>?, CancellationToken>(
                (content, _, _, _, _, _, _) =>
                {
                    using var copy = new MemoryStream();
                    content.CopyTo(copy);
                    stagedBytes = copy.ToArray();
                })
            .ReturnsAsync(StoredFileFactory.Create());

        var endpoint = Factory.Create<UploadHallOfFamePhotoEndpoint>(
            stagingServiceMock.Object, CreateFileStorageServiceMock().Object, CreateResizerMock(resized).Object);

        // Act
        await endpoint.HandleAsync(new UploadHallOfFamePhotoRequest { File = CreateFile() }, ct);

        // Assert
        stagedBytes.ShouldBe(new byte[] { 9, 8, 7 });
    }

    [Fact(DisplayName = "HandleAsync should return 422 and stage nothing when the file is not a readable image")]
    public async Task HandleAsync_ShouldReturnUnprocessable_WhenImageIsUnreadable()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var stagingServiceMock = new Mock<IUploadStagingService>(MockBehavior.Strict);
        var resizerMock = CreateResizerMock(ImageErrors.Unreadable);
        var endpoint = Factory.Create<UploadHallOfFamePhotoEndpoint>(stagingServiceMock.Object, CreateFileStorageServiceMock().Object, resizerMock.Object);

        // Act
        await endpoint.HandleAsync(new UploadHallOfFamePhotoRequest { File = CreateFile() }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register a permission-protected POST route under /hall-of-fame")]
    public void Configure_ShouldRegisterPermissionProtectedPostRoute_UnderHallOfFamePath()
    {
        // Arrange
        var stagingServiceMock = new Mock<IUploadStagingService>(MockBehavior.Strict);
        var resizerMock = new Mock<IImageResizer>(MockBehavior.Strict);
        var endpoint = Factory.Create<UploadHallOfFamePhotoEndpoint>(stagingServiceMock.Object, CreateFileStorageServiceMock().Object, resizerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("photo"), "should register the photo route");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("hall-of-fame"), "should be under the /hall-of-fame path");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}