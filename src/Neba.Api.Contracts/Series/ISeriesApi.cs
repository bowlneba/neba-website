using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Contracts.Series.CreateStageOverride;
using Neba.Api.Contracts.Series.EditSeries;
using Neba.Api.Contracts.Series.EditSeriesLogo;
using Neba.Api.Contracts.Series.GetSeries;
using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.Api.Contracts.Series.GetSeriesAdvancers;
using Neba.Api.Contracts.Series.ListSeries;
using Neba.Api.Contracts.Series.ListSeriesFinales;
using Neba.Api.Contracts.Series.ListStageResults;
using Neba.Api.Contracts.Series.ListStageSquads;
using Neba.Api.Contracts.Series.PickStageSquad;
using Neba.Api.Contracts.Series.RecomputeSeriesAdvancement;
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

    /// <summary>Gets each Stage's status, output and markers, and the Version writes must send. Requires Series.EditSeries.</summary>
    [Get("/series/{seriesId}/advancement")]
    Task<IApiResponse<SeriesAdvancementResponse>> GetSeriesAdvancementAsync(string seriesId, CancellationToken cancellationToken = default);

    /// <summary>Gets the bowlers who advanced out of each settled Stage. Public.</summary>
    [Get("/series/{seriesId}/advancers")]
    Task<IApiResponse<SeriesAdvancersResponse>> GetSeriesAdvancersAsync(string seriesId, CancellationToken cancellationToken = default);

    /// <summary>Recomputes from a Stage to the end. Requires Series.EditSeries.</summary>
    [Post("/series/{seriesId}/stages/{stageId}/recomputations")]
    Task<IApiResponse> RecomputeSeriesAdvancementAsync(string seriesId, string stageId, RecomputeSeriesAdvancementRequest request, CancellationToken cancellationToken = default);

    /// <summary>Picks a Survivor Stage's squad. Requires Series.EditSeries.</summary>
    [Put("/series/{seriesId}/stages/{stageId}/squad")]
    Task<IApiResponse> PickStageSquadAsync(string seriesId, string stageId, PickStageSquadRequest request, CancellationToken cancellationToken = default);

    /// <summary>Adds a bowler to a Qualifying Stage or clears a slot. Requires Series.ManageAdvancement.</summary>
    [Post("/series/{seriesId}/stages/{stageId}/overrides")]
    Task<IApiResponse<CreatedStageOverrideResponse>> CreateStageOverrideAsync(string seriesId, string stageId, CreateStageOverrideRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes an override. Requires Series.ManageAdvancement.</summary>
    [Delete("/series/{seriesId}/stages/{stageId}/overrides/{overrideId}")]
    Task<IApiResponse> RemoveStageOverrideAsync(string seriesId, string stageId, string overrideId, [Query] int version, CancellationToken cancellationToken = default);

    /// <summary>Lists the result rows of a Stage's tournament, for the add-bowler picker. Requires Series.ManageAdvancement.</summary>
    [Get("/series/{seriesId}/stages/{stageId}/results")]
    Task<IApiResponse<CollectionResponse<StageResultSummaryResponse>>> ListStageResultsAsync(string seriesId, string stageId, CancellationToken cancellationToken = default);

    /// <summary>Lists the squads of a Stage's tournament, for the squad picker. Requires Series.EditSeries.</summary>
    [Get("/series/{seriesId}/stages/{stageId}/squads")]
    Task<IApiResponse<CollectionResponse<StageSquadSummaryResponse>>> ListStageSquadsAsync(string seriesId, string stageId, CancellationToken cancellationToken = default);
}