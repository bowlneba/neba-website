using Refit;

namespace Neba.Api.Contracts.Awards;

/// <summary>
/// Defines the awards API contract.
/// </summary>
public interface IAwardsApi
{
    /// <summary>
    /// Lists the Bowler of the Year awards for all seasons.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// A collection of Bowler of the Year awards.
    /// </returns>
    [Get("/awards/bowler-of-the-year")]
    Task<IApiResponse<CollectionResponse<BowlerOfTheYearAwardResponse>>> ListBowlerOfTheYearAwardsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the High Block awards for all seasons.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// A collection of High Block awards.
    /// </returns>
    [Get("/awards/high-block")]
    Task<IApiResponse<CollectionResponse<HighBlockAwardResponse>>> ListHighBlockAwardsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the High Average awards for all seasons.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// A collection of High Average awards.
    /// </returns>
    [Get("/awards/high-average")]
    Task<IApiResponse<CollectionResponse<HighAverageAwardResponse>>> ListHighAverageAwardsAsync(CancellationToken cancellationToken = default);
}