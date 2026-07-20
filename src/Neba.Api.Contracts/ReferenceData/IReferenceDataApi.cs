using Refit;

namespace Neba.Api.Contracts.ReferenceData;

/// <summary>
/// Defines the reference data API contract — static/rarely-changing lookup lists (states, provinces,
/// phone number types, etc.) shared by every feature that needs them for form dropdowns.
/// </summary>
public interface IReferenceDataApi
{
    /// <summary>
    /// Lists all US states, including the District of Columbia.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// A collection of US states.
    /// </returns>
    [Get("/reference-data/us-states")]
    Task<IApiResponse<CollectionResponse<UsStateResponse>>> ListUsStatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all phone number types (e.g. Home, Mobile, Work, Fax).
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// A collection of phone number types.
    /// </returns>
    [Get("/reference-data/phone-number-types")]
    Task<IApiResponse<CollectionResponse<PhoneNumberTypeResponse>>> ListPhoneNumberTypesAsync(CancellationToken cancellationToken = default);
}
