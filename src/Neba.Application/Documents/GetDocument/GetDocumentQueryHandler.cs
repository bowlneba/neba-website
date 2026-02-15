using Neba.Application.Messaging;

namespace Neba.Application.Documents.GetDocument;

internal sealed class GetDocumentQueryHandler(IDocumentsService documentsService)
        : IQueryHandler<GetDocumentQuery, string>
{
    private readonly IDocumentsService _documentsService = documentsService;

    public Task<string> HandleAsync(GetDocumentQuery query, CancellationToken cancellationToken)
    {
        var document = _documentsService.GetDocumentAsHtmlAsync(query.DocumentName, cancellationToken);

        return document;
    }
}