using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Contracts.Series.EditSeries;
using Neba.Api.Contracts.Series.EditSeriesLogo;
using Neba.Api.Contracts.Series.GetSeries;
using Neba.Api.Contracts.Series.ListSeries;
using Neba.Api.Contracts.Series.ListSeriesFinales;
using Neba.Api.Contracts.Uploads;

using Refit;

namespace Neba.Api.Contracts.Series;

/// <summary>
/// Defines the Series API contract.
/// </summary>
public interface ISeriesApi
{
    /// <summary>Lists every Series, newest season first.</summary>
    [Get("/series")]
    Task<IApiResponse<CollectionResponse<SeriesSummaryResponse>>> ListSeriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists the Finale of every Series that has one, soonest first. Used by the tournament list.</summary>
    [Get("/series/finales")]
    Task<IApiResponse<CollectionResponse<SeriesFinaleSummaryResponse>>> ListSeriesFinalesAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a Series and its Stage schedule.</summary>
    [Get("/series/{seriesId}")]
    Task<IApiResponse<SeriesDetailResponse>> GetSeriesAsync(string seriesId, CancellationToken cancellationToken = default);

    /// <summary>Creates a Series. Requires Series.CreateSeries.</summary>
    [Post("/series")]
    Task<IApiResponse<CreatedSeriesResponse>> CreateSeriesAsync(CreateSeriesRequest request, CancellationToken cancellationToken = default);

    /// <summary>Saves a Series' name, full Stage list and Finale. Requires Series.EditSeries.</summary>
    [Put("/series/{seriesId}")]
    Task<IApiResponse> EditSeriesAsync(string seriesId, EditSeriesRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes an unlocked Series and its Stages. Requires Series.DeleteSeries.</summary>
    [Delete("/series/{seriesId}")]
    Task<IApiResponse> DeleteSeriesAsync(string seriesId, CancellationToken cancellationToken = default);

    /// <summary>Stages a logo file. Requires Series.CreateSeries, Series.EditSeries, or Series.EditSeriesLogo.</summary>
    [Multipart]
    [Post("/series/logo")]
    Task<IApiResponse<UploadedFileResponse>> UploadSeriesLogoAsync([AliasAs("file")] StreamPart file, CancellationToken cancellationToken = default);

    /// <summary>Replaces or removes the logo only. Requires Series.EditSeriesLogo. Works while locked.</summary>
    [Put("/series/{seriesId}/logo")]
    Task<IApiResponse> EditSeriesLogoAsync(string seriesId, EditSeriesLogoRequest request, CancellationToken cancellationToken = default);
}