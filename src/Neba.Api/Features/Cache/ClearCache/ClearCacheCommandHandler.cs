using ErrorOr;

using Microsoft.Extensions.Caching.Hybrid;

using Neba.Api.Documents;
using Neba.Api.Messaging;
using Neba.Api.Storage;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Cache.ClearCache;

internal sealed class ClearCacheCommandHandler(
    IFusionCache fusionCache,
    HybridCache hybridCache,
    IFileStorageService storageService,
    GoogleSettings googleSettings)
    : ICommandHandler<ClearCacheCommand>
{
    private const string CacheTag = "neba";

    public async Task<ErrorOr<Success>> HandleAsync(ClearCacheCommand command, CancellationToken cancellationToken)
    {
        await fusionCache.RemoveByTagAsync(CacheTag, token: cancellationToken);
        await hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);

        var deleteTasks = googleSettings.Documents
            .Select(doc => storageService.DeleteAsync(DocumentStorage.Container, DocumentStorage.BlobName(doc.Name), cancellationToken));
        await Task.WhenAll(deleteTasks);

        return Result.Success;
    }
}