namespace Neba.Api.Features.News.DeleteArticle;

/// <summary>
/// Represents a reference to a stored file that is associated with an article.
/// </summary>
public sealed record StoredFileReference
{
    /// <summary>
    /// Gets the name of the container where the file is stored.
    /// </summary>
    public required string Container { get; init; }

    /// <summary>
    /// Gets the path of the file within the container.
    /// </summary>
    public required string Path { get; init; }
}