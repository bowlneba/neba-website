using ErrorOr;

using Microsoft.AspNetCore.Http;

using Neba.Api.Storage;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Uploads;

[UnitTest]
[Component("Uploads")]
public sealed class LogoUploadProcessorTests
{
    private static readonly IReadOnlySet<string> AllowedTypes = new HashSet<string> { "image/png" };

    private static FormFile CreateFile(string fileName = "logo.jpeg")
        => new(new MemoryStream(new byte[1024]), 0, 1024, "File", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

    private static Mock<IImageResizer> CreateResizerMock(ErrorOr<ResizedImage> result)
    {
        var mock = new Mock<IImageResizer>(MockBehavior.Strict);
        mock.Setup(r => r.ResizeLogoAsync(
                It.IsAny<Stream>(),
                LogoUploadProcessor.MaxLogoDimension,
                AllowedTypes,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return mock;
    }

    [Fact(DisplayName = "ResizeAndStageAsync should stage the resized bytes under the folder and rename the file to the encoded format")]
    public async Task ResizeAndStageAsync_ShouldStageResizedFile()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var storedFile = StoredFileFactory.Create();
        var blobUri = new Uri("https://storage.example.com/test-container/logo.png");
        var resized = new ResizedImage { Content = [1, 2, 3], ContentType = "image/png", FileExtension = "png" };

        var staging = new Mock<IUploadStagingService>(MockBehavior.Strict);
        staging.Setup(s => s.StageUploadAsync(It.IsAny<Stream>(), "logo.png", "image/png", "bowlneba-public", "things/logo", null, ct)).ReturnsAsync(storedFile);
        var storage = new Mock<IFileStorageService>(MockBehavior.Strict);
        storage.Setup(s => s.GetBlobUri(storedFile.Container, storedFile.Path)).Returns(blobUri);
        var resizer = CreateResizerMock(resized);

        // Act
        var result = await LogoUploadProcessor.ResizeAndStageAsync(
            CreateFile(), "things/logo", AllowedTypes, resizer.Object, staging.Object, storage.Object, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.FileName.ShouldBe("logo.png");
        result.Value.Container.ShouldBe(storedFile.Container);
        result.Value.Path.ShouldBe(storedFile.Path);
        result.Value.ContentType.ShouldBe(storedFile.ContentType);
        result.Value.SizeInBytes.ShouldBe(storedFile.SizeInBytes);
        result.Value.Url.ShouldBe(blobUri);
    }

    [Fact(DisplayName = "ResizeAndStageAsync should return the resizer's errors and stage nothing")]
    public async Task ResizeAndStageAsync_ShouldReturnErrors_WhenResizerFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var staging = new Mock<IUploadStagingService>(MockBehavior.Strict);
        var storage = new Mock<IFileStorageService>(MockBehavior.Strict);
        var resizer = CreateResizerMock(ImageErrors.Unreadable);

        // Act
        var result = await LogoUploadProcessor.ResizeAndStageAsync(
            CreateFile(), "things/logo", AllowedTypes, resizer.Object, staging.Object, storage.Object, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(ImageErrors.Unreadable);
        staging.VerifyNoOtherCalls();
    }
}