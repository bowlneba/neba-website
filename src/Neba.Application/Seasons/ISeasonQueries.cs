namespace Neba.Application.Seasons;

/// <summary>
/// Defines queries for retrieving season data. This interface abstracts the underlying data retrieval mechanism, allowing for flexibility in implementation and easier testing. The GetAllAsync method retrieves a list of all seasons, ordered by start date descending, and is designed to be cached for 90 days due to the relatively static nature of season data.
/// </summary>
public interface ISeasonQueries
{
    /// <summary>
    /// Retrieves a list of all seasons, ordered by start date descending. This method is intended to be cached for 90 days, as season data is relatively static and infrequently updated. The returned collection contains summary information about each season, such as its name, start date, and end date, but does not include detailed information about the season's events or participants.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only collection of season DTOs.</returns>
    Task<IReadOnlyCollection<SeasonDto>> GetAllAsync(CancellationToken cancellationToken);
}