using ErrorOr;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace Neba.Api.Uploads;

internal sealed class ImageSharpResizer
    : IImageResizer
{
    /// <summary>
    /// The most pixels a side may have before decoding; a small file can still expand to a huge bitmap.
    /// </summary>
    internal const int MaxSourceSide = 12_000;

    public async Task<ErrorOr<ResizedImage>> ResizeAsync(
        Stream content,
        int maxDimension,
        IReadOnlySet<string> allowedContentTypes,
        CancellationToken cancellationToken)
    {
        // Buffered so the stream can be read twice (identify, then decode) whatever the caller passed.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        ImageInfo info;
        try
        {
            buffer.Position = 0;
            info = await Image.IdentifyAsync(buffer, cancellationToken);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            return ImageErrors.Unreadable;
        }

        var format = info.Metadata.DecodedImageFormat;
        if (format is null || !allowedContentTypes.Contains(format.DefaultMimeType))
        {
            return ImageErrors.UnsupportedFormat;
        }

        if (info.Width > MaxSourceSide || info.Height > MaxSourceSide)
        {
            return ImageErrors.TooLarge(MaxSourceSide);
        }

        try
        {
            buffer.Position = 0;
            using var image = await Image.LoadAsync(new DecoderOptions { MaxFrames = 1 }, buffer, cancellationToken);

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

            using var output = new MemoryStream();
            await image.SaveAsync(output, format, cancellationToken);

            return new ResizedImage
            {
                Content = output.ToArray(),
                ContentType = format.DefaultMimeType
            };
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            return ImageErrors.Unreadable;
        }
    }
}

internal static class ImageErrors
{
    public static Error Unreadable
        => Error.Validation("Image.Unreadable", "The file could not be read as an image.");

    public static Error UnsupportedFormat
        => Error.Validation("Image.UnsupportedFormat", "The file is not an allowed image type.");

    public static Error TooLarge(int maxSide)
        => Error.Validation("Image.TooLarge", $"The image must be no more than {maxSide} pixels on a side.");
}