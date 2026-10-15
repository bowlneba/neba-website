using Neba.Api.Contracts.Documents.GetDocument;

using Refit;

namespace Neba.Api.Contracts.Documents;

/// <summary>
/// Defines the documents API contract.
/// </summary>
public interface IDocumentsApi
{
    /// <summary>
    /// Gets a document by its name.
    /// </summary>
    /// <param name="documentName">
    /// The name of the document to retrieve.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// The requested document.
    /// </returns>
    [Get("/documents/{documentName}")]
    Task<IApiResponse<GetDocumentResponse>> GetDocumentAsync(string documentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears a document's cached copy so the next <see cref="GetDocumentAsync"/> call re-fetches it from Google Drive.
    /// </summary>
    /// <param name="documentName">
    /// The name of the document whose cached copy should be cleared.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    [Delete("/documents/{documentName}/cache")]
    Task<IApiResponse> RefreshDocumentAsync(string documentName, CancellationToken cancellationToken = default);
}