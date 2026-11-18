using ErrorOr;

using FastEndpoints;

using Microsoft.AspNetCore.Http;

using Neba.Api.Features.Series.UploadSeriesLogo;
using Neba.Api.Storage;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.Series.UploadSeriesLogo;

[UnitTest]
[Component("Series")]
public sealed class UploadSeriesLogoEndpointTests
{
    private static FormFile CreateFile()
        => new(new MemoryStream(new byte[1024]), 0, 1024, "File", "logo.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

    private static Mock<IImageResizer> CreateResizerMock(ErrorOr<ResizedImage> result)
    {
        var mock = new Mock<IImageResizer>(MockBehavior.Strict);
        mock.Setup(r => r.ResizeLogoAsync(
                It.IsAny<Stream>(),
                LogoUploadProcessor.MaxLogoDimension,
                UploadSeriesLogoRequestValidator.AllowedContentTypes,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return mock;
    }

    private static ResizedImage Resized()
        => new() { Content = [1, 2, 3], ContentType = "image/png", FileExtension = "png" };

    [Fact(DisplayName = "Configure should register a POST route at /series/logo behind the CanUploadSeriesLogo policy")]
    public void Configure_ShouldRegisterPostRouteWithPolicy()
    {
        // Arrange
        var staging = new Mock<IUploadStagingService>(MockBehavior.Strict);
        var storage = new Mock<IFileStorageService>(MockBehavior.Strict);
        var resizer = new Mock<IImageResizer>(MockBehavior.Strict);

        // Act
        var endpoint = Factory.Create<UploadSeriesLogoEndpoint>(staging.Object, storage.Object, resizer.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain("/series/logo");
        endpoint.Definition.PreBuiltUserPolicies!.ShouldContain("CanUploadSeriesLogo");
    }

    [Fact(DisplayName = "HandleAsync should stage the file under series/logo and return OK with the mapped file")]
    public async Task HandleAsync_ShouldStageFile_AndReturnMappedResponse()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var storedFile = StoredFileFactory.Create();
        var blobUri = new Uri("https://storage.example.com/test-container/logo.png");

        var staging = new Mock<IUploadStagingService>(MockBehavior.Strict);
        staging.Setup(s => s.StageUploadAsync(It.IsAny<Stream>(), "logo.png", "image/png", "bowlneba-public", "series/logo", null, ct)).ReturnsAsync(storedFile);
        var storage = new Mock<IFileStorageService>(MockBehavior.Strict);
        storage.Setup(s => s.GetBlobUri(storedFile.Container, storedFile.Path)).Returns(blobUri);
        var resizer = CreateResizerMock(Resized());
        var endpoint = Factory.Create<UploadSeriesLogoEndpoint>(staging.Object, storage.Object, resizer.Object);

        // Act
        await endpoint.HandleAsync(new UploadSeriesLogoRequest { File = CreateFile() }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Container.ShouldBe(storedFile.Container);
        endpoint.Response.Path.ShouldBe(storedFile.Path);
        endpoint.Response.FileName.ShouldBe("logo.png");
        endpoint.Response.ContentType.ShouldBe(storedFile.ContentType);
        endpoint.Response.SizeInBytes.ShouldBe(storedFile.SizeInBytes);
        endpoint.Response.Url.ShouldBe(blobUri);
    }

    [Fact(DisplayName = "HandleAsync should return 422 and stage nothing when the image cannot be read")]
    public async Task HandleAsync_ShouldReturn422_WhenResizerFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var staging = new Mock<IUploadStagingService>(MockBehavior.Strict);
        var storage = new Mock<IFileStorageService>(MockBehavior.Strict);
        var resizer = CreateResizerMock(ImageErrors.Unreadable);
        var endpoint = Factory.Create<UploadSeriesLogoEndpoint>(staging.Object, storage.Object, resizer.Object);

        // Act
        await endpoint.HandleAsync(new UploadSeriesLogoRequest { File = CreateFile() }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
        staging.VerifyNoOtherCalls();
    }
}