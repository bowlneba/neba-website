using Neba.Application.HallOfFame.ListHallOfFameInductions;

namespace Neba.Application.HallOfFame;

/// <summary>
/// Defines queries for retrieving Hall of Fame induction data.
/// </summary>
public interface IHallOfFameQueries
{
    /// <summary>
    /// Retrieves a list of all Hall of Fame inductions with summary information.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A collection of Hall of Fame induction DTOs.</returns>
    Task<IReadOnlyCollection<HallOfFameInductionDto>> GetAllAsync(CancellationToken cancellationToken);
}