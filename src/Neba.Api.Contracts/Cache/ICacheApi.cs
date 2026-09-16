using Refit;

namespace Neba.Api.Contracts.Cache;

/// <summary>Defines the Cache API contract for administrative cache management.</summary>
public interface ICacheApi
{
    /// <summary>Clears the API's L1/L2 cache and cached documents. Requires the Cache.Clear permission outside Development.</summary>
    [Delete("/cache")]
    Task<IApiResponse> ClearCacheAsync(CancellationToken cancellationToken = default);
}