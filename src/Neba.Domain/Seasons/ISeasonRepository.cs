namespace Neba.Domain.Seasons;

/// <summary>
/// Repository for accessing season data.
/// </summary>
public interface ISeasonRepository
{
    /// <summary>
    /// Gets a season by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the season.</param>
    /// <param name="trackChanges">Whether to track changes to the retrieved season entity. Defaults to true.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The season with the specified identifier, or null if no such season exists.</returns>
    Task<Season?> GetSeasonByIdAsync(SeasonId id, bool trackChanges = true, CancellationToken cancellationToken = default);
}