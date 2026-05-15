namespace Neba.Api.Features.Storage.Domain;

/// <summary>
/// The storage address of a file in Azure Blob Storage — container, path, content type, and size.
/// Does not hold file content.
/// </summary>
public sealed record StoredFile
{
    /// <summary>
    /// Gets the Azure Blob Storage container name where the file is stored.
    /// </summary>
    public string Container { get; init; } = string.Empty;

    /// <summary>
    /// Gets the blob path (key) within the container, including any virtual directory segments and file name.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Gets the MIME content type of the file (for example, "image/png").
    /// </summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the size of the file in bytes.
    /// </summary>
    public long SizeInBytes { get; init; }
}