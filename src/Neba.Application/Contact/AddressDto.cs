namespace Neba.Application.Contact;

/// <summary>
/// Data Transfer Object (DTO) for representing a postal address in application layers, such as API responses or service interfaces.
/// </summary>
public sealed record AddressDto
{
    /// <summary>
    /// Street address line, including house number and street name (required).
    /// </summary>
    public required string Street { get; init; }

    /// <summary>
    /// Optional unit, apartment, or suite number for the address; leave blank if not applicable.
    /// </summary>
    public string? Unit { get; init; }

    /// <summary>
    /// City or locality for the address (required).
    /// </summary>
    public required string City { get; init; }

    /// <summary>
    /// Region, state, or province for the address (required). For U.S. addresses, this should be a valid two-letter state code (e.g., "NY" for New York).
    /// </summary>
    public required string Region { get; init; }

    /// <summary>
    /// Country for the address (required). Use a valid ISO 3166-1 alpha-2 country code (e.g., "US" for United States) or a recognized country name. For U.S. addresses, this should be "United States".
    /// </summary>
    public required string Country { get; init; }

    /// <summary>
    /// Postal code or ZIP code for the address (required). For U.S. addresses, this should be a valid five-digit ZIP code (e.g., "10001").
    /// </summary>
    public required string PostalCode { get; init; }

    /// <summary>
    /// Latitude coordinate for the address (optional). Represents the geographic latitude in decimal degrees.
    /// </summary>
    public double? Latitude { get; init; }

    /// <summary>
    /// Longitude coordinate for the address (optional). Represents the geographic longitude in decimal degrees.
    /// </summary>
    public double? Longitude { get; init; }
}