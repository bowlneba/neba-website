namespace Neba.Api.Contracts.News;

/// <summary>
/// Represents the input required for a header image associated with a news article.
/// </summary>
public sealed record HeaderImageInput
{
    /// <summary>
    /// The storage container where the header image is located.
    /// </summary>
    public required string Container { get; init; }

    /// <summary>
    /// The path to the header image within the storage container.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// The MIME type of the header image.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// The size of the header image in bytes.
    /// </summary>
    public required long SizeInBytes { get; init; }
}