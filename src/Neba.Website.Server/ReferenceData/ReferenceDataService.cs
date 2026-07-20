using ErrorOr;

using Microsoft.Extensions.Caching.Memory;

using Neba.Api.Contracts.ReferenceData;
using Neba.Website.Server.Services;

namespace Neba.Website.Server.ReferenceData;

internal sealed class ReferenceDataService(
    ApiExecutor executor,
    IReferenceDataApi referenceDataApi,
    IMemoryCache cache) : IReferenceDataService
{
    // Cached client-side so page visits don't re-fetch static data; the API's own FusionCache layer only saves a network hop, not the round-trip itself.
    private const string UsStatesCacheKey = "neba:website:reference-data:us-states";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public async Task<ErrorOr<List<UsStateResponse>>> GetUsStatesAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(UsStatesCacheKey, out List<UsStateResponse>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await executor.ExecuteAsync(
            "ReferenceDataApi",
            nameof(GetUsStatesAsync),
            referenceDataApi.ListUsStatesAsync,
            ct);

        if (result.IsError) return result.Errors;

        var states = result.Value.Items.ToList();
        cache.Set(UsStatesCacheKey, states, CacheDuration);

        return states;
    }
}
