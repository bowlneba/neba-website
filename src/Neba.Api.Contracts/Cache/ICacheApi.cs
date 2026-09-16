using Refit;

namespace Neba.Api.Contracts.Cache;

/// <summary>Defines the Cache API contract for administrative cache management.</summary>
public interface ICacheApi
{
    /// <summary>Clears the API's L1/L2 cache. Requires the Cache.Clear permission outside Development.</summary>
    [Get("/debug/cache")]
    Task<IApiResponse<string>> ClearCacheAsync(CancellationToken cancellationToken = default);
}