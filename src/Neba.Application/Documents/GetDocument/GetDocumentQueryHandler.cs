using System.Globalization;

using ErrorOr;

using Neba.Application.Clock;
using Neba.Application.Messaging;
using Neba.Application.Storage;

namespace Neba.Application.Documents.GetDocument;

internal sealed class GetDocumentQueryHandler(
    IDocumentsService documentsService,
    IFileStorageService storageService,
    IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetDocumentQuery, ErrorOr<GetDocumentDto>>
{
    private readonly IDocumentsService _documentsService = documentsService;
    private readonly IFileStorageService _storageService = storageService;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public async Task<ErrorOr<GetDocumentDto>> HandleAsync(GetDocumentQuery query, CancellationToken cancellationToken)
    {
        if (await _storageService.ExistsAsync("documents", query.DocumentName, cancellationToken))
        {
            var file = await _storageService.GetFileAsync("documents", query.DocumentName, cancellationToken);

            if (file is null)
            {
                return DocumentErrors.DocumentNotFound(query.DocumentName);
            }

            DateTimeOffset? lastUpdated = null;
            if (file.Metadata.TryGetValue("source_last_modified", out var lastModifiedStr) &&
                DateTimeOffset.TryParse(lastModifiedStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            {
                lastUpdated = parsed;
            }

            return new GetDocumentDto { Html = file.Content, LastUpdated = lastUpdated };
        }

        var document = await _documentsService.GetDocumentAsHtmlAsync(query.DocumentName, cancellationToken);

        if (document is null)
        {
            return DocumentErrors.DocumentNotFound(query.DocumentName);
        }

        var now = _dateTimeProvider.UtcNow;

        await _storageService.UploadFileAsync(
            "documents",
            query.DocumentName,
            document.Content,
            document.ContentType,
            new Dictionary<string, string>
            {
                { "source_document_id", document.Id },
                { "cached_at", now.ToString("o") },
                { "source_last_modified", document.ModifiedAt?.ToString("o") ?? string.Empty }
            },
            cancellationToken);

        return new GetDocumentDto { Html = document.Content, LastUpdated = document.ModifiedAt };
    }
}