using Neba.Api.Contracts.HallOfFame.ListHallOfFameInductions;

using Refit;

namespace Neba.Api.Contracts.HallOfFame;

/// <summary>
/// Defines the Hall of Fame API contract.
/// </summary>
public interface IHallOfFameApi
{
    /// <summary>
    /// Lists all Hall of Fame inductions.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// A collection of Hall of Fame inductions.
    /// </returns>
    [Get("/hall-of-fame/inductions")]
    Task<IApiResponse<CollectionResponse<HallOfFameInductionResponse>>> ListHallOfFameInductionsAsync(CancellationToken cancellationToken = default);
}