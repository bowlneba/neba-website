using Refit;

namespace Neba.Api.Contracts.Sponsors;

/// <summary>
/// Defines the sponsors API contract.
/// </summary>
public interface ISponsorsApi
{
    /// <summary>
    /// Lists all active sponsors.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of active sponsor summaries.</returns>
    [Get("/sponsors/active")]
    Task<IApiResponse<CollectionResponse<SponsorSummaryResponse>>> ListActiveSponsorsAsync(CancellationToken cancellationToken = default);
}