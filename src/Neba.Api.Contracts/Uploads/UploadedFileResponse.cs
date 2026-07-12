namespace Neba.Api.Contracts.Uploads;

/// <summary>
/// The stored-file pointer returned after a successful upload.
/// </summary>
public sealed record UploadedFileResponse
{
    /// <summary>
    /// The blob storage container the file was uploaded to.
    /// </summary>
    public required string Container { get; init; }

    /// <summary>
    /// The blob path within the container.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// The file's original file name, as uploaded.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// The file's MIME content type as uploaded.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// The file's size in bytes.
    /// </summary>
    public required long SizeInBytes { get; init; }

    /// <summary>
    /// A directly-browsable URL for the uploaded file. Valid immediately, since the upload container is public.
    /// </summary>
    public required Uri Url { get; init; }
}