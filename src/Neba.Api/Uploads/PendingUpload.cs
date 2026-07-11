namespace Neba.Api.Uploads;

/// <summary>
/// Represents a pending file upload with its container, path, and upload timestamp.
/// </summary>
public sealed class PendingUpload
{
    /// <summary>
    /// Gets or sets the container name for the pending upload.
    /// </summary>
    public required string Container { get; set; }

    /// <summary>
    /// Gets or sets the path of the pending upload.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// Gets or sets the upload timestamp of the pending upload in UTC.
    /// </summary>
    public required DateTimeOffset UploadedAtUtc { get; set; }
}