using Neba.Application.Seasons;
using Neba.Domain.Seasons;

namespace Neba.Application.Tournaments;

/// <summary>
/// Defines queries related to tournaments, such as retrieving the number of tournaments in a given season.
/// </summary>
public interface ITournamentQueries
{
    /// <summary>
    /// Gets the number of tournaments that took place in a given season.  This will change to SeasonId when Tournaments are in the database
    /// </summary>
    /// <param name="seasonId">The ID of the season for which to count tournaments.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The number of tournaments in the specified season.</returns>
    Task<int> GetTournamentCountForSeasonAsync(SeasonId seasonId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a list of tournaments that took place in a given season.  This will change to SeasonId when Tournaments are in the database
    /// </summary>
    /// <param name="seasonId">The ID of the season for which to retrieve tournaments.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>A list of tournaments in the specified season.</returns>
    Task<IReadOnlyCollection<TournamentSummaryDto>> GetTournamentsInSeasonAsync(SeasonId seasonId, CancellationToken cancellationToken);
}