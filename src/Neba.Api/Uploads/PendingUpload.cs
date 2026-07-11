namespace Neba.Api.Uploads;

internal sealed class PendingUpload
{
    public required string Container { get; set; }

    public required string Path { get; set; }

    public required DateTimeOffset UploadedAtUtc { get; set; }
}