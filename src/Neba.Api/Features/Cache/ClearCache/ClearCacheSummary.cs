using FastEndpoints;

namespace Neba.Api.Features.Cache.ClearCache;

internal sealed class ClearCacheSummary : Summary<ClearCacheEndpoint>
{
    public ClearCacheSummary()
    {
        Summary = "Clears the API's cache and cached documents.";
        Description = "Evicts every entry tagged \"neba\" from both the L1/L2 FusionCache and the HybridCache stack, and deletes each configured Google document's cached copy from blob storage so the next request re-fetches fresh content. Anonymous in Development for local debugging; requires the Cache.Clear permission everywhere else.";

        Response(204, "Cache cleared.");
        Response(401, "No valid bearer token provided.");
        Response(403, "The caller lacks the Cache.Clear permission.");
    }
}