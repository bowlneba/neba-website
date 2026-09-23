using ErrorOr;

using Neba.Api.Caching;
using Neba.Api.Documents;
using Neba.Api.Messaging;
using Neba.Api.Storage;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Documents.RefreshDocument;

internal sealed class RefreshDocumentCommandHandler(
    IFusionCache fusionCache,
    IFileStorageService storageService,
    GoogleSettings googleSettings,
    ILogger<RefreshDocumentCommandHandler> logger)
        : ICommandHandler<RefreshDocumentCommand, Deleted>
{
    public async Task<ErrorOr<Deleted>> HandleAsync(RefreshDocumentCommand command, CancellationToken cancellationToken)
    {
        // Only documents listed in appsettings may be cleared. An unlisted name is still a 204 — the
        // caller's goal (that document isn't cached) already holds — but nothing is touched.
        if (!googleSettings.Documents.Any(document => string.Equals(document.Name, command.DocumentName, StringComparison.Ordinal)))
        {
            logger.LogRefreshRequestedForUnlistedDocument(command.DocumentName);

            return Result.Deleted;
        }

        await fusionCache.RemoveByTagAsync(CacheDescriptors.Documents.Tag(command.DocumentName), token: cancellationToken);
        await storageService.DeleteAsync(DocumentStorage.Container, DocumentStorage.BlobName(command.DocumentName), cancellationToken);

        return Result.Deleted;
    }
}
