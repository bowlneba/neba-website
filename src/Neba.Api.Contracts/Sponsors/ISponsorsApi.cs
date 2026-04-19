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
    [Get("/sponsors")]
    Task<IApiResponse<CollectionResponse<SponsorSummaryResponse>>> ListActiveSponsorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific sponsor by slug.
    /// </summary>
    /// <param name="slug">The slug of the sponsor.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The detailed information of the sponsor.</returns>
    [Get("/sponsors/{slug}")]
    Task<IApiResponse<SponsorDetailResponse>> GetSponsorBySlugAsync(string slug, CancellationToken cancellationToken = default);
}