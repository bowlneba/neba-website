using ErrorOr;

using Neba.Api.Caching;
using Neba.Api.Documents;
using Neba.Api.Messaging;
using Neba.Api.Storage;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Documents.RefreshDocument;

internal sealed class RefreshDocumentCommandHandler(
    IFusionCache fusionCache,
    IFileStorageService storageService)
        : ICommandHandler<RefreshDocumentCommand, Deleted>
{
    public async Task<ErrorOr<Deleted>> HandleAsync(RefreshDocumentCommand command, CancellationToken cancellationToken)
    {
        await fusionCache.RemoveByTagAsync(CacheDescriptors.Documents.Tag(command.DocumentName), token: cancellationToken);
        await storageService.DeleteAsync(DocumentStorage.Container, DocumentStorage.BlobName(command.DocumentName), cancellationToken);

        return Result.Deleted;
    }
}