using Neba.Api.Contracts.HallOfFame.EditHallOfFamePhoto;
using Neba.Api.Contracts.HallOfFame.ListHallOfFameInductions;
using Neba.Api.Contracts.Uploads;

using Refit;

namespace Neba.Api.Contracts.HallOfFame;

/// <summary>
/// Defines the Hall of Fame API contract.
/// </summary>
public interface IHallOfFameApi
{
    /// <summary>
    /// Lists all Hall of Fame inductions.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// A collection of Hall of Fame inductions.
    /// </returns>
    [Get("/hall-of-fame/inductions")]
    Task<IApiResponse<CollectionResponse<HallOfFameInductionResponse>>> ListHallOfFameInductionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a Hall of Fame photo to staging. Requires the HallOfFame.EditHallOfFamePhoto permission.
    /// </summary>
    /// <param name="file">
    /// The photo image file to upload.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// The uploaded file's metadata.
    /// </returns>
    [Multipart]
    [Post("/hall-of-fame/photos")]
    Task<IApiResponse<UploadedFileResponse>> UploadHallOfFamePhotoAsync(
        [AliasAs("File")] StreamPart file,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets, replaces, or removes an inductee's photo and its framing. Requires the HallOfFame.EditHallOfFamePhoto permission.
    /// </summary>
    /// <param name="id">
    /// The ULID string identifying the induction.
    /// </param>
    /// <param name="request">
    /// The photo change to apply.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    [Put("/hall-of-fame/{id}/photo")]
    Task<IApiResponse> EditHallOfFamePhotoAsync(string id, EditHallOfFamePhotoRequest request, CancellationToken cancellationToken = default);
}
