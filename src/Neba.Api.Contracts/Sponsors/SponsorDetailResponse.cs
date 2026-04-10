using Neba.Api.Contracts.Contact;
using Neba.Api.Contracts.OpenApi;

namespace Neba.Api.Contracts.Sponsors;

/// <summary>
/// Represents detailed sponsor information returned by the API, including business contact details and optional social/profile metadata.
/// </summary>
public sealed record SponsorDetailResponse
{
    /// <summary>
    /// Unique identifier for the sponsor.
    /// </summary>
    public required Ulid Id { get; init; }

    /// <summary>
    /// Display name of the sponsor.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// URL-friendly identifier for the sponsor.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Indicates whether the sponsor is currently active.
    /// </summary>
    public required bool IsCurrentSponsor { get; init; }

    /// <summary>
    /// Priority ordering value for display.
    /// </summary>
    public required int Priority { get; init; }

    /// <summary>
    /// Sponsorship tier.
    /// </summary>
    [OpenApiSmartEnum("SponsorTier")]
    public required string Tier { get; init; }

    /// <summary>
    /// Sponsor category.
    /// </summary>
    [OpenApiSmartEnum("SponsorCategory")]
    public required string Category { get; init; }

    /// <summary>
    /// Public URL of the sponsor's logo.
    /// </summary>
    public Uri? LogoUrl { get; init; }

    /// <summary>
    /// Public website URL for the sponsor.
    /// </summary>
    public Uri? WebsiteUrl { get; init; }

    /// <summary>
    /// Optional sponsor tagline.
    /// </summary>
    public string? TagPhrase { get; init; }

    /// <summary>
    /// Long-form sponsor description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional promotional notes provided for the sponsor.
    /// </summary>
    public string? PromotionalNotes { get; init; }

    /// <summary>
    /// Optional live-read script text for events.
    /// </summary>
    public string? LiveReadText { get; init; }

    /// <summary>
    /// URL to the sponsor's Facebook profile.
    /// </summary>
    public Uri? FacebookUrl { get; init; }

    /// <summary>
    /// URL to the sponsor's Instagram profile.
    /// </summary>
    public Uri? InstagramUrl { get; init; }

    /// <summary>
    /// Street line for the sponsor's business address.
    /// </summary>
    public string? BusinessStreet { get; init; }

    /// <summary>
    /// Optional suite or unit value for the sponsor's business address.
    /// </summary>
    public string? BusinessUnit { get; init; }

    /// <summary>
    /// City for the sponsor's business address.
    /// </summary>
    public string? BusinessCity { get; init; }

    /// <summary>
    /// Region or state for the sponsor's business address.
    /// </summary>
    [OpenApiSmartEnum("UsState")]
    public string? BusinessState { get; init; }

    /// <summary>
    /// Postal code for the sponsor's business address.
    /// </summary>
    public string? BusinessPostalCode { get; init; }

    /// <summary>
    /// Country for the sponsor's business address.
    /// </summary>
    [OpenApiSmartEnum("Country")]
    public string? BusinessCountry { get; init; }

    /// <summary>
    /// Business email address for sponsor inquiries.
    /// </summary>
    public string? BusinessEmailAddress { get; init; }

    /// <summary>
    /// Contact phone numbers associated with the sponsor.
    /// </summary>
    public required IReadOnlyCollection<PhoneNumberResponse> PhoneNumbers { get; init; }

    /// <summary>
    /// Name of the sponsor's primary contact person.
    /// </summary>
    public string? SponsorContactName { get; init; }

    /// <summary>
    /// Email address of the sponsor's primary contact person.
    /// </summary>
    public string? SponsorContactEmailAddress { get; init; }

    /// <summary>
    /// Phone number of the sponsor's primary contact person.
    /// </summary>
    public string? SponsorContactPhoneNumber { get; init; }

    /// <summary>
    /// Phone number type of the sponsor's primary contact person.
    /// </summary>
    [OpenApiSmartEnum("PhoneNumberType")]
    public string? SponsorContactPhoneNumberType { get; init; }
}