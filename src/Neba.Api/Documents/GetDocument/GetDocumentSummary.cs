using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Documents.GetDocument;

namespace Neba.Api.Documents.GetDocument;

internal sealed class GetDocumentSummary
    : Summary<GetDocumentEndpoint>
{
    public GetDocumentSummary()
    {
        Summary = "Gets a document by its name.";
        Description = "Retrieves the HTML content of a document by its unique name.";

        Response(200, "The document HTML content.",
            contentType: MediaTypeNames.Application.Json,
            example: new GetDocumentResponse
            {
                Html = "<html><body><h1>Document Title</h1><p>This is the document content.</p></body></html>",
                LastUpdated = new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero)
            });

        Response(404, "The document was not found.",
            contentType: MediaTypeNames.Application.Json,
            example: new
            {
                Errors = new[]
                {
                    new
                    {
                        Code = "Document.NotFound",
                        Description = "The specified document was not found."
                    }
                }
            });
    }
}