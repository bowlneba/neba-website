using Neba.Api.Contracts.Sponsors.CreateSponsor;
using Neba.Api.Contracts.Uploads;

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
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// A collection of active sponsor summaries.
    /// </returns>
    [Get("/sponsors")]
    Task<IApiResponse<CollectionResponse<SponsorSummaryResponse>>> ListActiveSponsorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific sponsor by slug.
    /// </summary>
    /// <param name="slug">
    /// The slug of the sponsor.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// The detailed information of the sponsor.
    /// </returns>
    [Get("/sponsors/{slug}")]
    Task<IApiResponse<SponsorDetailResponse>> GetSponsorBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a sponsor.
    /// </summary>
    /// <param name="request">
    /// The sponsor fields to create.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// The identifier and slug of the newly created sponsor.
    /// </returns>
    [Post("/sponsors")]
    Task<IApiResponse<SponsorResponse>> CreateSponsorAsync(CreateSponsorRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a sponsor logo. Requires the Sponsors.CreateSponsor permission.
    /// </summary>
    /// <param name="file">
    /// The logo image file to upload.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// The uploaded file's metadata.
    /// </returns>
    [Multipart]
    [Post("/sponsors/logo")]
    Task<IApiResponse<UploadedFileResponse>> UploadSponsorLogoAsync(
        [AliasAs("File")] StreamPart file,
        CancellationToken cancellationToken = default);
}