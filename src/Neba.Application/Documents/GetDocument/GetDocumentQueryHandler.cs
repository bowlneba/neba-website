using ErrorOr;

using Neba.Application.Clock;
using Neba.Application.Messaging;
using Neba.Application.Storage;

namespace Neba.Application.Documents.GetDocument;

internal sealed class GetDocumentQueryHandler(
    IDocumentsService documentsService,
    IFileStorageService storageService,
    IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetDocumentQuery, ErrorOr<string>>
{
    private readonly IDocumentsService _documentsService = documentsService;
    private readonly IFileStorageService _storageService = storageService;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public async Task<ErrorOr<string>> HandleAsync(GetDocumentQuery query, CancellationToken cancellationToken)
    {
        if (await _storageService.ExistsAsync("documents", query.DocumentName, cancellationToken))
        {
            var file = await _storageService.GetFileAsync("documents", query.DocumentName, cancellationToken);

            return file?.Content.ToErrorOr()
                ?? DocumentErrors.DocumentNotFound(query.DocumentName);
        }

        var document = await _documentsService.GetDocumentAsHtmlAsync(query.DocumentName, cancellationToken);

        if (document is null)
        {
            return DocumentErrors.DocumentNotFound(query.DocumentName);
        }

        // Cache the document content in storage for future requests (will be a background job when implemented)
        await _storageService.UploadFileAsync(
            "documents",
            query.DocumentName,
            document.Content,
            document.ContentType,
            new Dictionary<string, string>
            {
                { "source_document_id", document.Id },
                { "cached_at", _dateTimeProvider.UtcNow.ToString("o") }
            },
            cancellationToken);

        return document.Content;
    }
}