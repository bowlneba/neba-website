using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Metadata.Profiles.Iptc;
using SixLabors.ImageSharp.Metadata.Profiles.Xmp;
using SixLabors.ImageSharp.PixelFormats;

namespace Neba.Api.Tests.Uploads;

[UnitTest]
[Component("Uploads")]
public sealed class ImageSharpResizerTests
{
    private static readonly IReadOnlySet<string> Allowed = new HashSet<string> { "image/jpeg", "image/png", "image/webp", "image/gif" };

    private readonly FakeLogger<ImageSharpResizer> _logger = new();
    private readonly ImageSharpResizer _sut;

    public ImageSharpResizerTests()
        => _sut = new ImageSharpResizer(_logger);

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

    [Fact(DisplayName = "ResizeAsync should return Image.TooLarge when the pixel count is over the source limit")]
    public async Task ResizeAsync_ShouldReturnTooLarge_WhenPixelCountExceedsSourceLimit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var source = Png(8_000, 7_000); // each side is allowed; 56 MP in all is not

        // Act
        var result = await _sut.ResizeAsync(source, 100, Allowed, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Image.TooLarge");
    }

    [Fact(DisplayName = "ResizeAsync should return Image.Unreadable and log the exception when the image data is corrupt")]
    public async Task ResizeAsync_ShouldReturnUnreadableAndLog_WhenImageDataIsCorrupt()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var full = new MemoryStream();
        using (var image = new Image<Rgba32>(300, 300))
        {
            await image.SaveAsync(full, new PngEncoder(), ct);
        }

        // Flip a byte inside the first IDAT chunk so its CRC fails; whichever step reads it must return Unreadable, not throw.
        var bytes = full.ToArray();
        var idat = bytes.AsSpan().IndexOf("IDAT"u8);
        bytes[idat + 6] ^= 0xFF;
        await using var source = new MemoryStream(bytes);

        // Act
        var result = await _sut.ResizeAsync(source, 100, Allowed, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Image.Unreadable");
        _logger.Collector.GetSnapshot().ShouldContain(record => record.Level >= LogLevel.Information && record.Exception != null);
    }

    [Fact(DisplayName = "ResizeAsync should log when the bytes are not an image")]
    public async Task ResizeAsync_ShouldLogInformation_WhenBytesAreNotAnImage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var source = new MemoryStream("this is not an image"u8.ToArray());

        // Act
        await _sut.ResizeAsync(source, 100, Allowed, ct);

        // Assert
        _logger.Collector.GetSnapshot().ShouldContain(record => record.Level == LogLevel.Information && record.Exception != null);
    }

    [Fact(DisplayName = "ResizeAsync should read a forward-only stream")]
    public async Task ResizeAsync_ShouldResize_WhenStreamIsNotSeekable()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var source = new ForwardOnlyStream(Png(400, 200).ToArray());

        // Act
        var result = await _sut.ResizeAsync(source, 100, Allowed, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        Image.Identify(result.Value.Content).Width.ShouldBe(100);
    }

    [Theory(DisplayName = "ResizeAsync should keep the decoded format and report its content type and extension")]
    [InlineData("jpeg", "image/jpeg", "jpg")]
    [InlineData("png", "image/png", "png")]
    [InlineData("webp", "image/webp", "webp")]
    [InlineData("gif", "image/gif", "gif")]
    public async Task ResizeAsync_ShouldReportDecodedFormat(string format, string contentType, string extension)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var source = Encode(format, 40, 40);

        // Act
        var result = await _sut.ResizeAsync(source, 100, Allowed, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ContentType.ShouldBe(contentType);
        result.Value.FileExtension.ShouldBe(extension);
    }

    [Fact(DisplayName = "ResizeAsync should keep only the first frame of an animated GIF and log it")]
    public async Task ResizeAsync_ShouldKeepFirstFrame_WhenGifIsAnimated()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var animated = new Image<Rgba32>(20, 20);
        using (var second = new Image<Rgba32>(20, 20))
        {
            animated.Frames.AddFrame(second.Frames.RootFrame);
        }

        var stream = new MemoryStream();
        await animated.SaveAsync(stream, new GifEncoder(), ct);
        stream.Position = 0;

        // Act
        var result = await _sut.ResizeAsync(stream, 100, Allowed, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        Image.Identify(result.Value.Content).FrameMetadataCollection.Count.ShouldBe(1);
        _logger.Collector.GetSnapshot().ShouldContain(record => record.Level == LogLevel.Information && record.Message.Contains("2 frames"));
    }

    [Fact(DisplayName = "ResizeAsync should drop EXIF, XMP, and IPTC profiles")]
    public async Task ResizeAsync_ShouldDropAllMetadataProfiles()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var image = new Image<Rgba32>(20, 20);
        image.Metadata.ExifProfile = new ExifProfile();
        image.Metadata.ExifProfile.SetValue(ExifTag.Software, "camera-app");
        image.Metadata.XmpProfile = new XmpProfile("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"/>"u8.ToArray());
        image.Metadata.IptcProfile = new IptcProfile();
        image.Metadata.IptcProfile.SetValue(IptcTag.Caption, "private caption");
        var stream = new MemoryStream();
        await image.SaveAsync(stream, new JpegEncoder(), ct);
        stream.Position = 0;

        // Act
        var result = await _sut.ResizeAsync(stream, 100, Allowed, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var info = Image.Identify(result.Value.Content);
        info.Metadata.XmpProfile.ShouldBeNull();
        info.Metadata.IptcProfile.ShouldBeNull();
        info.Metadata.ExifProfile?.Values.ShouldNotContain(value => value.Tag == ExifTag.Software);
    }

    private static MemoryStream Encode(string format, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var stream = new MemoryStream();
        switch (format)
        {
            case "jpeg": image.Save(stream, new JpegEncoder()); break;
            case "png": image.Save(stream, new PngEncoder()); break;
            case "webp": image.Save(stream, new WebpEncoder()); break;
            case "gif": image.Save(stream, new GifEncoder()); break;
            default: throw new ArgumentOutOfRangeException(nameof(format));
        }

        stream.Position = 0;
        return stream;
    }

    private sealed class ForwardOnlyStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
    }
}