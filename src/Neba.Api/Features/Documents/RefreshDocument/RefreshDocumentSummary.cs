using FastEndpoints;

namespace Neba.Api.Features.Documents.RefreshDocument;

internal sealed class RefreshDocumentSummary : Summary<RefreshDocumentEndpoint>
{
    public RefreshDocumentSummary()
    {
        Summary = "Clears a single document's cached copy.";
        Description = "Evicts the document's FusionCache entry and deletes its cached copy from blob storage, so the next request to GetDocument re-fetches it from Google Drive. Requires the Documents.RefreshDocument permission.";

        Response(204, "Document cache cleared.");
        Response(401, "No valid bearer token provided.");
        Response(403, "The caller lacks the Documents.RefreshDocument permission.");
    }
}