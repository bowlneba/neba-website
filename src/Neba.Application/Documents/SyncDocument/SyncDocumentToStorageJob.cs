using Neba.Application.BackgroundJobs;

namespace Neba.Application.Documents.SyncDocument;

/// <summary>
/// Represents a background job for synchronizing a document to storage.
/// </summary>
public sealed record SyncDocumentToStorageJob
    : IBackgroundJob
{
    /// <summary>
    /// Gets the name of the document to be synchronized.
    /// </summary>
    public required string DocumentName { get; init; }

    /// <summary>
    /// Gets the identifier of the user who triggered the synchronization.
    /// </summary>
    public required string TriggeredBy { get; init; }

    ///<inheritdoc />
    public string JobName
        => $"SyncDocumentToStorage: {DocumentName}";
}