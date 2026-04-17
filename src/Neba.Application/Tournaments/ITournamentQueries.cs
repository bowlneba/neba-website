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
    /// <param name="season"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<int> GetTournamentCountForSeasonAsync(SeasonDto season, CancellationToken cancellationToken);
}