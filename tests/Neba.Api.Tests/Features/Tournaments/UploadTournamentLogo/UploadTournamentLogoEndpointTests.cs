using FastEndpoints;

using Microsoft.AspNetCore.Http;

using Neba.Api.Contracts.Tournaments.UploadTournamentLogo;
using Neba.Api.Features.Tournaments.UploadTournamentLogo;
using Neba.Api.Storage;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.Tournaments.UploadTournamentLogo;

[UnitTest]
[Component("Tournaments")]
public sealed class UploadTournamentLogoEndpointTests
{
    private static IFormFile CreateFile(string contentType = "image/png", long length = 1024)
    {
        var stream = new MemoryStream(new byte[length]);
        return new FormFile(stream, 0, length, "File", "logo.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static Mock<IFileStorageService> CreateFileStorageServiceMock(Uri? blobUri = null)
    {
        var mock = new Mock<IFileStorageService>(MockBehavior.Strict);
        mock.Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(blobUri ?? new Uri("https://storage.example.com/test-container/logo.png"));
        return mock;
    }

    [Fact(DisplayName = "HandleAsync should return OK with the staged file mapped to the response")]
    public async Task HandleAsync_ShouldReturnOkWithMappedFile_WhenStagingSucceeds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var storedFile = StoredFileFactory.Create();
        var blobUri = new Uri("https://storage.example.com/test-container/logo.png");

        var stagingServiceMock = new Mock<IUploadStagingService>(MockBehavior.Strict);
        stagingServiceMock
            .Setup(s => s.StageUploadAsync(It.IsAny<IFormFile>(), "bowlneba-public", "tournaments/logo", null, ct))
            .ReturnsAsync(storedFile);

        var fileStorageServiceMock = CreateFileStorageServiceMock(blobUri);

        var endpoint = Factory.Create<UploadTournamentLogoEndpoint>(stagingServiceMock.Object, fileStorageServiceMock.Object);
        var file = CreateFile();
        await using var stream = file.OpenReadStream();

        // Act
        await endpoint.HandleAsync(new UploadTournamentLogoRequest { File = file }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Container.ShouldBe(storedFile.Container);
        endpoint.Response.Path.ShouldBe(storedFile.Path);
        endpoint.Response.FileName.ShouldBe("logo.png");
        endpoint.Response.ContentType.ShouldBe(storedFile.ContentType);
        endpoint.Response.SizeInBytes.ShouldBe(storedFile.SizeInBytes);
        endpoint.Response.Url.ShouldBe(blobUri);
    }

    [Fact(DisplayName = "HandleAsync should stage the request file under the tournaments/logo container and prefix")]
    public async Task HandleAsync_ShouldStageRequestFile_UnderTournamentsLogoContainerAndPrefix()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var file = CreateFile();
        var request = new UploadTournamentLogoRequest { File = file };

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

        var endpoint = Factory.Create<UploadTournamentLogoEndpoint>(stagingServiceMock.Object, CreateFileStorageServiceMock().Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        capturedFile.ShouldBeSameAs(file);
        capturedContainer.ShouldBe("bowlneba-public");
        capturedPathPrefix.ShouldBe("tournaments/logo");
        capturedMetadata.ShouldBeNull();
    }

    [Fact(DisplayName = "Configure should register a permission-protected POST route under /tournaments")]
    public void Configure_ShouldRegisterPermissionProtectedPostRoute_UnderTournamentsPath()
    {
        // Arrange
        var stagingServiceMock = new Mock<IUploadStagingService>(MockBehavior.Strict);
        var endpoint = Factory.Create<UploadTournamentLogoEndpoint>(stagingServiceMock.Object, CreateFileStorageServiceMock().Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("logo"), "should register the logo route");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("tournaments"), "should be under the /tournaments path");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}
