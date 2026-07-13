namespace Neba.Api.Uploads;

/// <summary>
/// Tracks a file uploaded to blob storage before its owning record (e.g. an article) was saved. A
/// system/staging record, not a domain entity — removed once claimed, or by
/// <see cref="CleanupOrphanedUploadsJob"/> if never claimed.
/// </summary>
public sealed class PendingUpload
{
    /// <summary>
    /// The container name for the pending upload.
    /// </summary>
    public required string Container { get; init; }

    /// <summary>
    /// The path of the pending upload.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// The upload timestamp of the pending upload in UTC.
    /// </summary>
    public required DateTimeOffset UploadedAtUtc { get; init; }
}