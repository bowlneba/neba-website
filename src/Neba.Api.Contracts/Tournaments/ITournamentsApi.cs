using Neba.Api.Contracts.Tournaments.AddTournamentSponsor;
using Neba.Api.Contracts.Tournaments.CreateTournament;
using Neba.Api.Contracts.Tournaments.EditTournament;
using Neba.Api.Contracts.Tournaments.GetTournament;
using Neba.Api.Contracts.Tournaments.ListChampions;
using Neba.Api.Contracts.Tournaments.ListTournamentTypes;
using Neba.Api.Contracts.Uploads;

using Refit;

namespace Neba.Api.Contracts.Tournaments;

/// <summary>
/// Defines the tournaments API contract.
/// </summary>
public interface ITournamentsApi
{
    /// <summary>
    /// Gets full details for a single tournament.
    /// </summary>
    [Get("/tournaments/{tournamentId}")]
    Task<IApiResponse<TournamentDetailResponse>> GetTournamentAsync(
        string tournamentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a tournament.
    /// </summary>
    [Post("/tournaments")]
    Task<IApiResponse<CreatedTournamentResponse>> CreateTournamentAsync(
        CreateTournamentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of tournament champions. This includes the bowler's name, the tournament they won, and other relevant details.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of tournament champions.</returns>
    [Get("/tournaments/champions")]
    Task<IApiResponse<CollectionResponse<TournamentChampionResponse>>> ListTournamentChampionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of active tournament types, for use in tournament creation pickers.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of active tournament types.</returns>
    [Get("/tournaments/types")]
    Task<IApiResponse<CollectionResponse<TournamentTypeSummaryResponse>>> ListTournamentTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a tournament logo. Requires the Tournaments.CreateTournament permission.
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
    [Post("/tournaments/logo")]
    Task<IApiResponse<UploadedFileResponse>> UploadTournamentLogoAsync(
        [AliasAs("File")] StreamPart file,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a sponsor to a tournament.
    /// </summary>
    [Post("/tournaments/{id}/sponsors")]
    Task<IApiResponse> AddTournamentSponsorAsync(string id, AddTournamentSponsorRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a sponsor from a tournament.
    /// </summary>
    [Delete("/tournaments/{id}/sponsors/{sponsorId}")]
    Task<IApiResponse> RemoveTournamentSponsorAsync(string id, string sponsorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Edits an existing tournament.
    /// </summary>
    [Put("/tournaments/{id}")]
    Task<IApiResponse> EditTournamentAsync(string id, EditTournamentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a tournament. Returns 204 whether or not a tournament with the given ID existed. Refuses with 409
    /// if the tournament has recorded championship, entry, or result history.
    /// </summary>
    [Delete("/tournaments/{id}")]
    Task<IApiResponse> DeleteTournamentAsync(string id, CancellationToken cancellationToken = default);
}