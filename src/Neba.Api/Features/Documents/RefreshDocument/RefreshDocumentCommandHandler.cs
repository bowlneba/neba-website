using ErrorOr;

using Neba.Api.Messaging;
using Neba.Api.Storage;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Documents.RefreshDocument;

internal sealed class RefreshDocumentCommandHandler(
    IFusionCache fusionCache,
    IFileStorageService storageService)
        : ICommandHandler<RefreshDocumentCommand, Deleted>
{
    private const string DocumentsContainer = "bowlneba-private";

    public async Task<ErrorOr<Deleted>> HandleAsync(RefreshDocumentCommand command, CancellationToken cancellationToken)
    {
        await fusionCache.RemoveByTagAsync($"neba:document:{command.DocumentName}", token: cancellationToken);
        await storageService.DeleteAsync(DocumentsContainer, $"documents/{command.DocumentName}", cancellationToken);

        return Result.Deleted;
    }
}
