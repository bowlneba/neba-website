namespace Neba.Application.Storage;

/// <summary>
/// Represents a file retrieved from storage, including its content, content type, and metadata.
/// </summary>
public sealed record StoredFile
{
    /// <summary>
    /// The content of the file as a string. For binary files, this may be a Base64-encoded string or similar representation, depending on the storage implementation.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The MIME type of the file, such as "text/plain" for a text file or "image/png" for a PNG image. This information can be used to determine how to handle or display the file content.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// A dictionary of metadata associated with the file. This can include any additional information about the file, such as custom properties, tags, or other relevant data that may be useful for processing or categorizing the file. The specific metadata included will depend on the storage implementation and how files are uploaded or managed within the system.
    /// </summary>
    public required IDictionary<string, string> Metadata { get; init; }
}