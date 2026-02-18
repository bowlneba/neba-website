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

            DateTimeOffset? cachedAt = null;
            if (file.Metadata.TryGetValue("cached_at", out var cachedAtStr) &&
                DateTimeOffset.TryParse(cachedAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            {
                cachedAt = parsed;
            }

            return new GetDocumentDto { Html = file.Content, CachedAt = cachedAt };
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
                { "cached_at", now.ToString("o") }
            },
            cancellationToken);

        return new GetDocumentDto { Html = document.Content, CachedAt = now };
    }
}
