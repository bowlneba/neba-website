namespace Neba.Application.Storage;


/// <summary>
/// Service for storing and retrieving files from Azure Blob Storage.
/// </summary>
/// <remarks>
/// Files are the storage representation of any content within NEBA, regardless of origin.
/// See docs/ubiquitous-language.md for the formal definition of "File" in the NEBA domain.
/// </remarks>
public interface IFileStorageService
{
    /// <summary>
    /// Checks whether a file exists in the specified container.
    /// </summary>
    /// <param name="container">The storage container name (e.g., "documents").</param>
    /// <param name="path">The blob path within the container.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns><c>true</c> if the file exists; otherwise, <c>false</c>.</returns>
    Task<bool> ExistsAsync(string container, string path, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a file's content and metadata from the specified container.
    /// </summary>
    /// <param name="container">The storage container name.</param>
    /// <param name="path">The blob path within the container.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The file content and metadata, or <c>null</c> if the file does not exist.</returns>
    Task<StoredFile?> GetFileAsync(string container, string path, CancellationToken cancellationToken);

    /// <summary>
    /// Uploads content as a file to the specified container.
    /// </summary>
    /// <param name="container">The storage container name.</param>
    /// <param name="path">The blob path within the container.</param>
    /// <param name="content">The file content.</param>
    /// <param name="contentType">The MIME content type (e.g., "text/html").</param>
    /// <param name="metadata">Key-value metadata to associate with the file.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task UploadFileAsync(
        string container,
        string path,
        string content,
        string contentType,
        IDictionary<string, string> metadata,
        CancellationToken cancellationToken);
}