namespace Neba.Api.Uploads;

/// <summary>
/// Tracks a file uploaded to blob storage before its owning record (e.g. an article) was saved. A
/// system/staging record, not a domain entity — removed once claimed, or by
/// <see cref="CleanupOrphanedUploadsJob"/> if never claimed.
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