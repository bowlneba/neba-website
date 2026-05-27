using Neba.Api.Contracts.Bowlers.GetBowlerTitles;

using Refit;

namespace Neba.Api.Contracts.Bowlers;

/// <summary>
/// Defines the bowlers API contract.
/// </summary>
public interface IBowlersApi
{
    /// <summary>
    /// Retrieves the titles won by a specific bowler, including details about each tournament.
    /// </summary>
    /// <param name="bowlerId">The unique identifier of the bowler.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A response containing the bowler's titles.</returns>
    [Get("/bowlers/{bowlerId}/titles")]
    Task<IApiResponse<BowlerTitlesResponse>> GetBowlerTitlesAsync(string bowlerId, CancellationToken cancellationToken = default);
}