using ErrorOr;

using Neba.Api.Contracts.Uploads;
using Neba.Api.Storage;

namespace Neba.Api.Uploads;

/// <summary>
/// Crops, resizes, and stages an uploaded logo. The logo upload endpoints share it.
/// </summary>
internal static class LogoUploadProcessor
{
    /// <summary>
    /// The longest side a stored logo keeps. The largest tile is 96px, so 3x covers a high-density screen.
    /// </summary>
    internal const int MaxLogoDimension = 512;

    /// <summary>
    /// Resizes the logo and stages it in the public container under <paramref name="folder"/>.
    /// </summary>
    /// <param name="file">The uploaded file.</param>
    /// <param name="folder">The staging folder, such as <c>series/logo</c>.</param>
    /// <param name="allowedContentTypes">The image types the endpoint accepts.</param>
    /// <param name="imageResizer">The image resizer.</param>
    /// <param name="stagingService">The upload staging service.</param>
    /// <param name="fileStorageService">The file storage service.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The staged file, or the resizer's validation errors.</returns>
    internal static async Task<ErrorOr<UploadedFileResponse>> ResizeAndStageAsync(
        IFormFile file,
        string folder,
        IReadOnlySet<string> allowedContentTypes,
        IImageResizer imageResizer,
        IUploadStagingService stagingService,
        IFileStorageService fileStorageService,
        CancellationToken cancellationToken)
    {
        await using var upload = file.OpenReadStream();
        var resized = await imageResizer.ResizeLogoAsync(upload, MaxLogoDimension, allowedContentTypes, cancellationToken);

        if (resized.IsError)
        {
            return resized.Errors;
        }

        // The resizer re-encodes in the format the bytes decoded as, which can differ from the client's file name.
        var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}.{resized.Value.FileExtension}";

        await using var content = new MemoryStream(resized.Value.Content);
        var storedFile = await stagingService.StageUploadAsync(
            content, fileName, resized.Value.ContentType, "bowlneba-public", folder, null, cancellationToken);

        return new UploadedFileResponse
        {
            Container = storedFile.Container,
            Path = storedFile.Path,
            FileName = fileName,
            ContentType = storedFile.ContentType,
            SizeInBytes = storedFile.SizeInBytes,
            Url = fileStorageService.GetBlobUri(storedFile.Container, storedFile.Path),
        };
    }
}
