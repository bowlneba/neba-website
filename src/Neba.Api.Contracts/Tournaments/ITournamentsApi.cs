using Neba.Api.Contracts.Tournaments.GetTournament;

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
}