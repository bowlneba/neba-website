using Neba.Domain.Bowlers;

namespace Neba.Application.Bowlers;

/// <summary>
/// Interface for querying bowler-related data, such as retrieving mappings of legacy bowler IDs to current bowler IDs. This is used to support features that require referencing bowlers by their legacy identifiers, ensuring compatibility with historical data and systems that may still use those identifiers.
/// </summary>
public interface IBowlerQueries
{
    /// <summary>
    /// Retrieves a mapping of legacy bowler IDs to current bowler IDs. This allows the application to translate between old identifiers used in historical data and the current identifiers used in the system, facilitating data consistency and integration across different parts of the application that may rely on legacy data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A dictionary mapping legacy bowler IDs to current bowler IDs.</returns>
    Task<IReadOnlyDictionary<int, BowlerId>> GetBowlerIdByLegacyIdAsync(CancellationToken cancellationToken);
}