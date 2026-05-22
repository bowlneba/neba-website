using Neba.Api.Contracts.Tournaments.GetTournament;
using Neba.Api.Contracts.Tournaments.ListChampions;

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
    /// Gets a list of tournament champions. This includes the bowler's name, the tournament they won, and other relevant details.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of tournament champions.</returns>
    [Get("/tournaments/champions")]
    Task<ICollectionResponse<TournamentChampionResponse>> ListTournamentChampionsAsync(CancellationToken cancellationToken = default);
}