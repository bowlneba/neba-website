using Neba.Application.BowlingCenters.ListBowlingCenters;

namespace Neba.Application.BowlingCenters;

/// <summary>
/// Defines queries for retrieving bowling center data.
/// </summary>
public interface IBowlingCenterQueries
{
    /// <summary>
    /// Retrieves a list of all bowling centers with summary information.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only collection of bowling center summary DTOs.</returns>
    Task<IReadOnlyCollection<BowlingCenterSummaryDto>> GetAllAsync(CancellationToken cancellationToken);
}