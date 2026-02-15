
namespace Neba.Application.Documents;

/// <summary>
/// Service for retrieving documents from external document management systems (Google Drive, Microsoft 365, etc.).
/// </summary>
/// <remarks>
/// Documents are organizational content sourced from external systems and exported for web display.
/// Examples include bylaws, tournament rules, officer handbooks, and policy documents.
/// See docs/ubiquitous-language.md for the formal definition of "Document" in the NEBA domain.
/// </remarks>
public interface IDocumentsService
{
    /// <summary>
    /// Retrieves a document as HTML by its configured name.
    /// </summary>
    /// <param name="documentName">
    /// The logical name of the document as configured in application settings.
    /// This is a case-insensitive lookup key (e.g., "bylaws", "tournament-rules").
    /// </param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// The document content as processed HTML ready for web display,
    /// or <c>null</c> if the document name is not found in configuration.
    /// HTML includes:
    /// - Body content only (no full HTML document structure)
    /// - Generated anchor IDs on headings for deep linking
    /// - Internal routes replacing external document system URLs
    /// </returns>
    Task<string?> GetDocumentAsHtmlAsync(string documentName, CancellationToken cancellationToken);
}