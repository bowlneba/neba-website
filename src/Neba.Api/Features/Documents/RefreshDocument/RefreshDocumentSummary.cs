using FastEndpoints;

namespace Neba.Api.Features.Documents.RefreshDocument;

internal sealed class RefreshDocumentSummary : Summary<RefreshDocumentEndpoint>
{
    public RefreshDocumentSummary()
    {
        Summary = "Clears a single document's cached copy.";
        Description = "Evicts the document's FusionCache entry and deletes its cached copy from blob storage, so the next request to GetDocument re-fetches it from Google Drive. Only documents listed under Google:Documents in appsettings are cleared; any other name returns 204 without clearing anything and logs a warning. Requires the Documents.RefreshDocument permission.";

        Response(204, "Document cache cleared, or the name isn't a configured document and nothing was cleared.");
        Response(400, "DocumentName is missing or longer than 100 characters.");
        Response(401, "No valid bearer token provided.");
        Response(403, "The caller lacks the Documents.RefreshDocument permission.");
    }
}