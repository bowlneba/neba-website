using Neba.Api.Contracts.Contact;

namespace Neba.Api.Contracts.BowlingCenters.ListBowlingCenters;

/// <summary>
/// Represents a summary of a bowling center's key details for display in lists and search results, including contact information and geolocation data for mapping. This response model is designed to provide clients with the necessary information to identify and locate bowling centers, as well as to contact them directly if needed. The inclusion of geolocation data allows for enhanced user experiences, such as displaying the center on a map or providing directions.
/// </summary>
public sealed record BowlingCenterSummaryResponse
{
    /// <summary>
    /// The center's official certification number as issued by the governing body, used for verification and display purposes. This unique identifier helps users confirm the legitimacy of the bowling center and can be used for reference in communications or when searching for specific centers.
    /// </summary>
    public required string CertificationNumber { get; init; }

    /// <summary>
    /// The center's public-facing name as provided by the operator, used for display in lists and search results. This is the primary identifier that users will recognize and use when searching for or referring to the bowling center. It is important for this name to be accurate and consistent with what is displayed on the center's signage and marketing materials.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The current operational status of the bowling center, indicating whether it is open, temporarily closed, permanently closed, or under renovation. This information is crucial for users to know before planning a visit, as it helps them avoid unnecessary trips and allows them to make informed decisions about which centers to visit. The status should be updated regularly to reflect any changes in the center's operational state.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// The center's physical address details, including street, city, state, and postal code, used for display and geolocation purposes. This information is essential for users to find the center and for mapping features. The address should be formatted in a way that is easily understandable and consistent with standard postal formats to ensure that users can easily locate the center using GPS or mapping services.
    /// </summary>
    public required string Street { get; init; }

    /// <summary>
    /// An optional unit or suite number for the center's address, used for display and geolocation purposes. This information is important for centers located in multi-unit buildings or complexes, as it helps users identify the specific location of the bowling center within the larger property. If the center does not have a unit number, this field can be left null.
    /// </summary>
    public string? Unit { get; init; }

    /// <summary>
    /// The city where the bowling center is located, used for display and geolocation purposes. This information helps users identify the general location of the center and can be used for filtering search results by city. It is important for this field to be accurate and consistent with the city name as recognized by postal services and mapping applications.
    /// </summary>
    public required string City { get; init; }

    /// <summary>
    /// The state or province where the bowling center is located, used for display and geolocation purposes. This information helps users identify the broader location of the center and can be used for filtering search results by state. It is important for this field to be accurate and consistent with the state or province name as recognized by postal services and mapping applications.
    /// </summary>
    public required string State { get; init; }

    /// <summary>
    /// The postal code for the center's location, used for display and geolocation purposes. This information is essential for users to find the center and for mapping features, as it helps narrow down the location to a specific area. The postal code should be accurate and consistent with the format recognized by postal services to ensure that users can easily locate the center using GPS or mapping services.
    /// </summary>
    public required string PostalCode { get; init; }

    /// <summary>
    /// The latitude coordinate of the center's location, used for geolocation and mapping purposes. This information is essential for accurately placing the center on a map and for calculating distances to the center from the user's location.
    /// </summary>
    public required double Latitude { get; init; }

    /// <summary>
    /// The longitude coordinate of the center's location, used for geolocation and mapping purposes. This information is essential for accurately placing the center on a map and for calculating distances to the center from the user's location.
    /// </summary>
    public required double Longitude { get; init; }

    /// <summary>
    /// A collection of phone numbers associated with the bowling center, used for contact and communication purposes. This information is essential for users to reach the center for inquiries, reservations, or other services.
    /// </summary>
    public required IReadOnlyCollection<PhoneNumberResponse> PhoneNumbers { get; init; }
}