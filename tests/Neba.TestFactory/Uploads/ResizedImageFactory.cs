using Neba.Api.Uploads;

namespace Neba.TestFactory.Uploads;

internal static class ResizedImageFactory
{
    public const string ValidContentType = "image/jpeg";
    public const string ValidFileExtension = "jpg";
    public static readonly byte[] ValidContent = [1, 2, 3, 4];

    public static ResizedImage Create(
        byte[]? content = null,
        string? contentType = null,
        string? fileExtension = null)
        => new()
        {
            Content = content ?? ValidContent,
            ContentType = contentType ?? ValidContentType,
            FileExtension = fileExtension ?? ValidFileExtension
        };
}