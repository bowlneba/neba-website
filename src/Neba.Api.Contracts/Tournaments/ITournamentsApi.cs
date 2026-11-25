using Neba.Api.Contracts.Seasons.ListTournamentsInSeason;
using Neba.Api.Contracts.Tournaments.AddTournamentSponsor;
using Neba.Api.Contracts.Tournaments.CheckTeamEligibility;
using Neba.Api.Contracts.Tournaments.CreateTournament;
using Neba.Api.Contracts.Tournaments.EditTournament;
using Neba.Api.Contracts.Tournaments.EditTournamentLogo;
using Neba.Api.Contracts.Tournaments.EditTournamentSponsorAmount;
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
    /// Checks whether a team of bowlers may enter a tournament under Rules 401 to 403.
    /// </summary>
    /// <param name="tournamentId">The tournament the team wants to enter.</param>
    /// <param name="bowlerIds">The IDs of the bowlers found in the system.</param>
    /// <param name="unknownBowlerCount">How many team members were not found in the system.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    [Get("/tournaments/{tournamentId}/team-eligibility")]
    Task<IApiResponse<TeamEligibilityResponse>> CheckTeamEligibilityAsync(
        string tournamentId,
        [Query(CollectionFormat.Multi)] IEnumerable<string> bowlerIds,
        int unknownBowlerCount,
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
    /// Gets every tournament that has not ended yet, across the current and later seasons, ordered by start date.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of upcoming tournaments.</returns>
    [Get("/tournaments/upcoming")]
    Task<IApiResponse<CollectionResponse<SeasonTournamentResponse>>> ListUpcomingTournamentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of active tournament types, for use in tournament creation pickers.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of active tournament types.</returns>
    [Get("/tournaments/types")]
    Task<IApiResponse<CollectionResponse<TournamentTypeSummaryResponse>>> ListTournamentTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a tournament logo. Requires the Tournaments.CreateTournament, Tournaments.EditTournament,
    /// or Tournaments.EditTournamentLogo permission.
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
    /// Changes the sponsorship amount of a sponsor attached to a tournament.
    /// </summary>
    [Put("/tournaments/{id}/sponsors/{sponsorId}")]
    Task<IApiResponse> EditTournamentSponsorAmountAsync(string id, string sponsorId, EditTournamentSponsorAmountRequest request, CancellationToken cancellationToken = default);

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
    /// Replaces or removes a tournament's logo. Requires the Tournaments.EditTournamentLogo permission.
    /// </summary>
    [Put("/tournaments/{id}/logo")]
    Task<IApiResponse> EditTournamentLogoAsync(string id, EditTournamentLogoRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a tournament. Returns 204 whether or not a tournament with the given ID existed. Refuses with 409
    /// if the tournament has recorded championship, entry, or result history.
    /// </summary>
    [Delete("/tournaments/{id}")]
    Task<IApiResponse> DeleteTournamentAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a tournament truncated — held, but didn't finish as planned. Never title-eligible.
    /// </summary>
    [Patch("/tournaments/{id}/truncate")]
    Task<IApiResponse> TruncateTournamentAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a tournament cancelled — no official NEBA event took place under this record.
    /// </summary>
    [Patch("/tournaments/{id}/cancel")]
    Task<IApiResponse> CancelTournamentAsync(string id, CancellationToken cancellationToken = default);
}