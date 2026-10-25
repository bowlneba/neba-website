using ErrorOr;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.Processing;

namespace Neba.Api.Uploads;

internal sealed class ImageSharpResizer(ILogger<ImageSharpResizer> logger)
    : IImageResizer
{
    /// <summary>
    /// The most pixels a side may have before decoding; a small file can still expand to a huge bitmap.
    /// </summary>
    internal const int MaxSourceSide = 12_000;

    /// <summary>
    /// The most pixels an image may have in total before decoding. At 4 bytes a pixel this caps a decoded
    /// bitmap near 200 MB; the side limit alone would allow about 576 MB.
    /// </summary>
    internal const long MaxSourcePixels = 50_000_000;

    public async Task<ErrorOr<ResizedImage>> ResizeAsync(
        Stream content,
        int maxDimension,
        IReadOnlySet<string> allowedContentTypes,
        CancellationToken cancellationToken)
    {
        // A seekable stream is read in place (identify, rewind, decode); only a forward-only one is copied.
        MemoryStream? copy = null;
        var source = content;
        if (!content.CanSeek)
        {
            copy = new MemoryStream();
            await content.CopyToAsync(copy, cancellationToken);
            source = copy;
        }

        await using (copy)
        {
            ImageInfo info;
            try
            {
                source.Position = 0;
                info = await Image.IdentifyAsync(source, cancellationToken);
            }
            catch (Exception ex) when (IsUnreadableImage(ex))
            {
                logger.LogImageIdentifyFailed(ex);
                return ImageErrors.Unreadable;
            }

            var format = info.Metadata.DecodedImageFormat;
            if (format is null || !allowedContentTypes.Contains(format.DefaultMimeType))
            {
                logger.LogImageFormatRejected(format?.DefaultMimeType);
                return ImageErrors.UnsupportedFormat;
            }

            if (info.Width > MaxSourceSide || info.Height > MaxSourceSide || (long)info.Width * info.Height > MaxSourcePixels)
            {
                logger.LogImageTooLarge(info.Width, info.Height);
                return ImageErrors.TooLarge(MaxSourceSide, MaxSourcePixels);
            }

            if (info.FrameMetadataCollection.Count > 1)
            {
                logger.LogImageFramesDropped(info.FrameMetadataCollection.Count, format.DefaultMimeType);
            }

            try
            {
                source.Position = 0;
                using var image = await Image.LoadAsync(new DecoderOptions { MaxFrames = 1 }, source, cancellationToken);

                image.Mutate(context =>
                {
                    context.AutoOrient();

                    if (image.Width > maxDimension || image.Height > maxDimension)
                    {
                        context.Resize(new ResizeOptions
                        {
                            Size = new Size(maxDimension, maxDimension),
                            Mode = ResizeMode.Max
                        });
                    }
                });

                image.Metadata.ExifProfile = null;
                image.Metadata.XmpProfile = null;
                image.Metadata.IptcProfile = null;

                await using var output = new MemoryStream();
                await image.SaveAsync(output, format, cancellationToken);

                return new ResizedImage
                {
                    Content = output.ToArray(),
                    ContentType = format.DefaultMimeType,
                    FileExtension = format.FileExtensions.First()
                };
            }
            catch (Exception ex) when (IsUnreadableImage(ex))
            {
                logger.LogImageDecodeFailed(ex);
                return ImageErrors.Unreadable;
            }
        }
    }

    /// <summary>
    /// True for the failures a damaged or hostile file causes in the decoder or encoder: a malformed
    /// stream can surface as an ImageSharp error, a truncated read, a bad index or size, or a refused allocation.
    /// </summary>
    private static bool IsUnreadableImage(Exception ex)
        => ex is ImageProcessingException
            or UnknownImageFormatException
            or InvalidImageContentException
            or InvalidMemoryOperationException
            or NotSupportedException
            or EndOfStreamException
            or IndexOutOfRangeException
            or OverflowException;
}

internal static partial class ImageSharpResizerLogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded file could not be identified as an image.")]
    public static partial void LogImageIdentifyFailed(this ILogger<ImageSharpResizer> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Uploaded image was identified but failed to decode, resize, or encode.")]
    public static partial void LogImageDecodeFailed(this ILogger<ImageSharpResizer> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded image rejected: format '{ContentType}' is not allowed.")]
    public static partial void LogImageFormatRejected(this ILogger<ImageSharpResizer> logger, string? contentType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded image rejected: {Width}x{Height} is over the source size limit.")]
    public static partial void LogImageTooLarge(this ILogger<ImageSharpResizer> logger, int width, int height);

    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded {ContentType} image has {FrameCount} frames; only the first is kept.")]
    public static partial void LogImageFramesDropped(this ILogger<ImageSharpResizer> logger, int frameCount, string contentType);
}

internal static class ImageErrors
{
    public static Error Unreadable
        => Error.Validation("Image.Unreadable", "The file could not be read as an image.");

    public static Error UnsupportedFormat
        => Error.Validation("Image.UnsupportedFormat", "The file is not an allowed image type.");

    public static Error TooLarge(int maxSide, long maxPixels)
        => Error.Validation("Image.TooLarge", $"The image must be no more than {maxSide} pixels on a side and {maxPixels / 1_000_000} megapixels in all.");
}