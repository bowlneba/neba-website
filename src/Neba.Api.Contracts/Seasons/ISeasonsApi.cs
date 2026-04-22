using Neba.Api.Contracts.Seasons.ListSeasons;

using Refit;

namespace Neba.Api.Contracts.Seasons;

/// <summary>
/// Defines the seasons API contract.
/// </summary>
public interface ISeasonsApi
{
    /// <summary>
    /// Lists all seasons.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of seasons.</returns>
    [Get("/seasons")]
    Task<IApiResponse<CollectionResponse<SeasonResponse>>> ListSeasonsAsync(CancellationToken cancellationToken = default);
}