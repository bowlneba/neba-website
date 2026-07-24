using Neba.Api.Contracts.OilPatterns.CreateOilPattern;
using Neba.Api.Contracts.OilPatterns.ListOilPatterns;

using Refit;

namespace Neba.Api.Contracts.OilPatterns;

/// <summary>
/// Defines the oil patterns API contract.
/// </summary>
public interface IOilPatternsApi
{
    /// <summary>
    /// Creates an oil pattern.
    /// </summary>
    /// <param name="request">
    /// The fields required to create the oil pattern.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// The created oil pattern, including its derived lane-condition categories.
    /// </returns>
    [Post("/oil-patterns")]
    Task<IApiResponse<CreatedOilPatternResponse>> CreateOilPatternAsync(
        [Body] CreateOilPatternRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all oil patterns.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// A collection of oil pattern summaries.
    /// </returns>
    [Get("/oil-patterns")]
    Task<IApiResponse<CollectionResponse<OilPatternSummaryResponse>>> ListOilPatternsAsync(CancellationToken cancellationToken = default);
}