namespace Neba.Application.Awards;

/// <summary>
/// Defines query operations for retrieving season awards data, such as High Block awards.
/// </summary>
public interface IAwardQueries
{
    /// <summary>
    /// Asynchronously retrieves a collection of High Block awards for the current season, including the bowler's name and score.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of High Block award DTOs.</returns>
    Task<IReadOnlyCollection<HighBlockAwardDto>> GetAllHighBlockAwardsAsync(CancellationToken cancellationToken);
}