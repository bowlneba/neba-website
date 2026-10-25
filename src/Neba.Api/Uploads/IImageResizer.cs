using ErrorOr;

namespace Neba.Api.Uploads;

/// <summary>
/// Shrinks an uploaded image before it is stored.
/// </summary>
internal interface IImageResizer
{
    /// <summary>
    /// Decodes the image, applies its EXIF orientation, shrinks it to fit within <paramref name="maxDimension"/>
    /// on its longest side (it is never enlarged), drops EXIF, XMP, and IPTC metadata, and encodes it in its own format.
    /// A multi-frame image keeps only its first frame.
    /// </summary>
    /// <param name="content">The uploaded bytes.</param>
    /// <param name="maxDimension">The most pixels the longest side may keep.</param>
    /// <param name="allowedContentTypes">The image types to accept, judged by the decoded format and not the client's header.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The resized image, or a validation error when the bytes are not an allowed image.</returns>
    Task<ErrorOr<ResizedImage>> ResizeAsync(
        Stream content,
        int maxDimension,
        IReadOnlySet<string> allowedContentTypes,
        CancellationToken cancellationToken);
}

/// <summary>
/// An image after <see cref="IImageResizer.ResizeAsync"/>.
/// </summary>
internal sealed record ResizedImage
{
    /// <summary>
    /// The encoded image bytes.
    /// </summary>
    public required byte[] Content { get; init; }

    /// <summary>
    /// The content type of <see cref="Content"/>, from the format the bytes decoded as.
    /// </summary>
    public required string ContentType { get; init; }
}