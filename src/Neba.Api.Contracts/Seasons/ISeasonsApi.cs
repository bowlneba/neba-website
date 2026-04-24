using Neba.Api.Contracts.Seasons.ListSeasons;
using Neba.Api.Contracts.Seasons.ListTournamentsInSeason;

using Refit;

namespace Neba.Api.Contracts.Seasons;

/// <summary>
/// Defines the seasons API contract.
/// </summary>
public interface ISeasonsApi
{
    /// <summary>
    /// Lists all seasons.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of seasons.</returns>
    [Get("/seasons")]
    Task<IApiResponse<CollectionResponse<SeasonResponse>>> ListSeasonsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all tournaments in a season.
    /// </summary>
    /// <param name="seasonId">The ULID of the season.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of tournament summaries.</returns>
    [Get("/seasons/{seasonId}/tournaments")]
    Task<IApiResponse<CollectionResponse<SeasonTournamentResponse>>> ListTournamentsInSeasonAsync(
        string seasonId,
        CancellationToken cancellationToken = default);
}