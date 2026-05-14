using Neba.Application.Contact;

namespace Neba.Application.BowlingCenters.ListBowlingCenters;

/// <summary>
/// Represents a summary of a bowling center's key details for display in lists and search results, including contact information and geolocation data for mapping.
/// </summary>
public sealed record BowlingCenterSummaryDto
{
    /// <summary>
    /// The center's official certification number as issued by the governing body, used for verification and display purposes.
    /// </summary>
    public required string CertificationNumber { get; init; }

    /// <summary>
    /// The center's public-facing name as provided by the operator, used for display in lists and search results.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The current operational status of the bowling center, indicating whether it is open, temporarily closed, permanently closed, or under renovation. This information is crucial for users to know before planning a visit.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// The center's physical address details, including street, city, state, and postal code, used for display and geolocation purposes. This information is essential for users to find the center and for mapping features.
    /// </summary>
    public required AddressDto Address { get; init; }

    /// <summary>
    /// A collection of the center's primary contact phone numbers, including area codes and extensions when applicable, used for display and user contact purposes. This allows users to easily reach the center for inquiries or reservations.
    /// </summary>
    public IReadOnlyCollection<PhoneNumberDto> PhoneNumbers { get; init; } = [];

    /// <summary>
    /// The center's public website URL. Optional — not all centers have a website on file.
    /// </summary>
    public string? Website { get; init; }
}