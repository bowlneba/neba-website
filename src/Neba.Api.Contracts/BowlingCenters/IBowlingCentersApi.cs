
using Neba.Api.Contracts.BowlingCenters.ListBowlingCenters;

using Refit;

namespace Neba.Api.Contracts.BowlingCenters;

/// <summary>
/// Defines the bowling centers API contract.
/// </summary>
public interface IBowlingCentersApi
{
    /// <summary>
    /// Lists all bowling centers.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of bowling center summaries.</returns>
    [Get("/bowling-centers")]
    Task<IApiResponse<CollectionResponse<BowlingCenterSummaryResponse>>> ListBowlingCentersAsync(CancellationToken cancellationToken = default);
}