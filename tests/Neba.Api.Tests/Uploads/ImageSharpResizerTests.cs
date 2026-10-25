using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace Neba.Api.Tests.Uploads;

[UnitTest]
[Component("Uploads")]
public sealed class ImageSharpResizerTests
{
    private static readonly IReadOnlySet<string> Allowed = new HashSet<string> { "image/jpeg", "image/png", "image/webp", "image/gif" };

    private readonly ImageSharpResizer _sut = new();

    private static MemoryStream Png(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream JpegWithExif(int width, int height, ushort orientation)
    {
        using var image = new Image<Rgba32>(width, height);
        image.Metadata.ExifProfile = new ExifProfile();
        image.Metadata.ExifProfile.SetValue(ExifTag.Orientation, orientation);
        image.Metadata.ExifProfile.SetValue(ExifTag.Software, "camera-app");
        var stream = new MemoryStream();
        image.Save(stream, new JpegEncoder());
        stream.Position = 0;
        return stream;
    }

    [Fact(DisplayName = "ResizeAsync should shrink an image to fit the longest side, keeping the aspect ratio")]
    public async Task ResizeAsync_ShouldShrinkToFitLongestSide_WhenImageIsLarger()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var source = Png(400, 200);

        // Act
        var result = await _sut.ResizeAsync(source, 100, Allowed, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ContentType.ShouldBe("image/png");
        var info = Image.Identify(result.Value.Content);
        info.Width.ShouldBe(100);
        info.Height.ShouldBe(50);
    }

    [Fact(DisplayName = "ResizeAsync should not enlarge an image that already fits")]
    public async Task ResizeAsync_ShouldNotEnlarge_WhenImageAlreadyFits()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var source = Png(60, 40);

        // Act
        var result = await _sut.ResizeAsync(source, 100, Allowed, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var info = Image.Identify(result.Value.Content);
        info.Width.ShouldBe(60);
        info.Height.ShouldBe(40);
    }

    [Fact(DisplayName = "ResizeAsync should apply the EXIF orientation and drop the EXIF profile")]
    public async Task ResizeAsync_ShouldApplyOrientationAndDropExif_WhenImageHasExif()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var source = JpegWithExif(200, 100, orientation: 6); // rotate 90 degrees to display

        // Act
        var result = await _sut.ResizeAsync(source, 1000, Allowed, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ContentType.ShouldBe("image/jpeg");
        var info = Image.Identify(result.Value.Content);
        info.Width.ShouldBe(100);
        info.Height.ShouldBe(200);
        info.Metadata.ExifProfile?.Values.ShouldNotContain(value => value.Tag == ExifTag.Software);
    }

    [Fact(DisplayName = "ResizeAsync should return Image.Unreadable when the bytes are not an image")]
    public async Task ResizeAsync_ShouldReturnUnreadable_WhenBytesAreNotAnImage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var source = new MemoryStream("this is not an image"u8.ToArray());

        // Act
        var result = await _sut.ResizeAsync(source, 100, Allowed, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Image.Unreadable");
    }

    [Fact(DisplayName = "ResizeAsync should return Image.UnsupportedFormat when the decoded format is not allowed")]
    public async Task ResizeAsync_ShouldReturnUnsupportedFormat_WhenFormatIsNotAllowed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var source = Png(10, 10);
        IReadOnlySet<string> onlyJpeg = new HashSet<string> { "image/jpeg" };

        // Act
        var result = await _sut.ResizeAsync(source, 100, onlyJpeg, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Image.UnsupportedFormat");
    }

    [Fact(DisplayName = "ResizeAsync should return Image.TooLarge when a side is over the source limit")]
    public async Task ResizeAsync_ShouldReturnTooLarge_WhenSideExceedsSourceLimit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var source = Png(ImageSharpResizer.MaxSourceSide + 1, 1);

        // Act
        var result = await _sut.ResizeAsync(source, 100, Allowed, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Image.TooLarge");
    }
}