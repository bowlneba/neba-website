using ErrorOr;

using Neba.Application.Messaging;

namespace Neba.Application.Documents.GetDocument;

internal sealed class GetDocumentQueryHandler(IDocumentsService documentsService)
        : IQueryHandler<GetDocumentQuery, ErrorOr<string>>
{
    private readonly IDocumentsService _documentsService = documentsService;

    public async Task<ErrorOr<string>> HandleAsync(GetDocumentQuery query, CancellationToken cancellationToken)
    {
        var document = await _documentsService.GetDocumentAsHtmlAsync(query.DocumentName, cancellationToken);

        return document?.Content.ToErrorOr()
            ?? DocumentErrors.DocumentNotFound(query.DocumentName);
    }
}