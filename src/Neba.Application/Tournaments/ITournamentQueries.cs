using Neba.Application.Seasons;
using Neba.Application.Tournaments.ListTournamentsInSeason;
using Neba.Domain.Seasons;
using Neba.Domain.Tournaments;

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
    Task<IReadOnlyCollection<SeasonTournamentDto>> GetTournamentsInSeasonAsync(SeasonId seasonId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the number of entries for each tournament in the provided list of tournament IDs.
    /// </summary>
    /// <param name="tournamentIds">The IDs of the tournaments for which to count entries.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>A dictionary mapping each tournament ID to its corresponding entry count.</returns>
    Task<IReadOnlyDictionary<TournamentId, int>> GetTournamentEntryCountsAsync(IEnumerable<TournamentId> tournamentIds, CancellationToken cancellationToken);
}