using Neba.Api.BackgroundJobs;

namespace Neba.Api.Features.News.DeleteArticle;

/// <summary>
/// Represents a background job that deletes files associated with an article.
/// </summary>
public sealed record DeleteArticleFilesJob
    : IBackgroundJob
{
    /// <summary>
    /// Gets the collection of files to be deleted.
    /// </summary>
    public required IReadOnlyCollection<StoredFileReference> Files { get; init; }

    /// <inheritdoc />
    public string JobName
        => $"{nameof(DeleteArticleFilesJob)}: {Files.Count} file(s)";
}