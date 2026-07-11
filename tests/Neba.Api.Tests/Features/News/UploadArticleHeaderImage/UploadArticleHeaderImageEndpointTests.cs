using FastEndpoints;

using Microsoft.AspNetCore.Http;

using Neba.Api.Contracts.News.UploadArticleHeaderImage;
using Neba.Api.Features.News.UploadArticleHeaderImage;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.News.UploadArticleHeaderImage;

[UnitTest]
[Component("News")]
public sealed class UploadArticleHeaderImageEndpointTests
{
    private static IFormFile CreateFile(string contentType = "image/png", long length = 1024)
    {
        var stream = new MemoryStream(new byte[length]);
        return new FormFile(stream, 0, length, "File", "header.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact(DisplayName = "HandleAsync should return OK with the staged file mapped to the response")]
    public async Task HandleAsync_ShouldReturnOkWithMappedFile_WhenStagingSucceeds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var storedFile = StoredFileFactory.Create();

        var stagingServiceMock = new Mock<IUploadStagingService>(MockBehavior.Strict);
        stagingServiceMock
            .Setup(s => s.StageUploadAsync(It.IsAny<IFormFile>(), "news", "header", null, ct))
            .ReturnsAsync(storedFile);

        var endpoint = Factory.Create<UploadArticleHeaderImageEndpoint>(stagingServiceMock.Object);

        // Act
        await endpoint.HandleAsync(new UploadArticleHeaderImageRequest { File = CreateFile() }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Container.ShouldBe(storedFile.Container);
        endpoint.Response.Path.ShouldBe(storedFile.Path);
        endpoint.Response.ContentType.ShouldBe(storedFile.ContentType);
        endpoint.Response.SizeInBytes.ShouldBe(storedFile.SizeInBytes);
    }

    [Fact(DisplayName = "HandleAsync should stage the request file under the news/header container and prefix")]
    public async Task HandleAsync_ShouldStageRequestFile_UnderNewsHeaderContainerAndPrefix()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var file = CreateFile();
        var request = new UploadArticleHeaderImageRequest { File = file };

        IFormFile? capturedFile = null;
        string? capturedContainer = null;
        string? capturedPathPrefix = null;
        IDictionary<string, string>? capturedMetadata = null;

        var stagingServiceMock = new Mock<IUploadStagingService>(MockBehavior.Strict);
        stagingServiceMock
            .Setup(s => s.StageUploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>?>(), ct))
            .Callback<IFormFile, string, string, IDictionary<string, string>?, CancellationToken>(
                (f, container, pathPrefix, metadata, _) =>
                {
                    capturedFile = f;
                    capturedContainer = container;
                    capturedPathPrefix = pathPrefix;
                    capturedMetadata = metadata;
                })
            .ReturnsAsync(StoredFileFactory.Create());

        var endpoint = Factory.Create<UploadArticleHeaderImageEndpoint>(stagingServiceMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        capturedFile.ShouldBeSameAs(file);
        capturedContainer.ShouldBe("news");
        capturedPathPrefix.ShouldBe("header");
        capturedMetadata.ShouldBeNull();
    }

    [Fact(DisplayName = "Configure should register a permission-protected POST route under /news")]
    public void Configure_ShouldRegisterPermissionProtectedPostRoute_UnderNewsPath()
    {
        // Arrange
        var stagingServiceMock = new Mock<IUploadStagingService>(MockBehavior.Strict);
        var endpoint = Factory.Create<UploadArticleHeaderImageEndpoint>(stagingServiceMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("header-image"), "should register the header-image route");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("news"), "should be under the /news path");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}
