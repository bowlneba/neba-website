using Neba.Api.Contracts.OpenApi;

namespace Neba.Api.Contracts.Contact;

/// <summary>
/// Represents a phone number associated with a contact, including the type of phone number (e.g., mobile, landline, fax) and the actual phone number string. This information is used for display and contact purposes in the API responses.
/// </summary>
public sealed record PhoneNumberResponse
{
    /// <summary>
    /// The type of phone number, such as "mobile", "landline", "fax", etc., used for categorizing the phone number and providing context to users. This information helps users understand the nature of the contact number and how it can be used.
    /// </summary>
    [OpenApiSmartEnum("PhoneNumberType")]
    public required string PhoneNumberType { get; init; }

    /// <summary>
    /// The actual phone number string, including area code and any extensions if applicable, used for display and contact purposes. This is the primary piece of information that users will use to contact the center or individual associated with this phone number.
    /// </summary>
    public required string PhoneNumber { get; init; }
}