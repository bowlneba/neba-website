using Neba.Api.Contracts.Contact;

namespace Neba.Website.Server.Sponsors;

/// <summary>
/// View model representing detailed sponsor information for display on the sponsor detail page, including business contact details and optional social/profile metadata.
/// </summary>
public sealed record SponsorDetailViewModel
{
    /// <summary>
    /// Unique identifier for the sponsor.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// URL-friendly identifier for the sponsor.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Display name of the sponsor.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Indicates whether the sponsor is currently active.
    /// </summary>
    public required bool IsCurrentSponsor { get; init; }

    /// <summary>
    /// Priority ordering value for display.
    /// </summary>
    public required string TierName { get; init; }

    /// <summary>
    /// Sponsor category.
    /// </summary>
    public required string CategoryName { get; init; }

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
    public string? Tagline { get; init; }

    /// <summary>
    /// Detailed description or about text for the sponsor.
    /// </summary>
    public string? AboutText { get; init; }

    /// <summary>
    /// Optional script for live reads or announcements related to the sponsor.
    /// </summary>
    public string? PromotionalNotes { get; init; }

    /// <summary>
    /// Optional script for live reads or announcements related to the sponsor.
    /// </summary>
    public string? LiveReadScript { get; init; }

    /// <summary>
    /// Optional Facebook profile URL for the sponsor.
    /// </summary>
    public Uri? FacebookUrl { get; init; }

    /// <summary>
    /// Optional Instagram profile URL for the sponsor.
    /// </summary>
    public Uri? InstagramUrl { get; init; }

    /// <summary>
    /// Business address details for the sponsor, if provided. Displayed on the contact card if any address fields are present.
    /// </summary>
    public string? BusinessStreet { get; init; }

    /// <summary>
    /// Optional unit/suite number for the sponsor's business address.
    /// </summary>
    public string? BusinessUnit { get; init; }

    /// <summary>
    /// City for the sponsor's business address.
    /// </summary>
    public string? BusinessCity { get; init; }

    /// <summary>
    /// State or region for the sponsor's business address.
    /// </summary>
    public string? BusinessState { get; init; }

    /// <summary>
    /// Postal code for the sponsor's business address.
    /// </summary>
    public string? BusinessPostalCode { get; init; }

    /// <summary>
    /// Country for the sponsor's business address.
    /// </summary>
    public string? BusinessCountry { get; init; }

    /// <summary>
    /// Business email address for contacting the sponsor. Displayed on the contact card if provided.
    /// </summary>
    public string? ContactEmail { get; init; }

    /// <summary>
    /// Collection of phone numbers for contacting the sponsor, including type (e.g. mobile, office). Displayed on the contact card if any phone numbers are provided.
    /// </summary>
    public required IReadOnlyCollection<PhoneNumberResponse> PhoneNumbers { get; init; }

    /// <summary>
    /// Optional internal contact name for the sponsor, intended for authenticated users only. Displayed in a separate section on the sponsor detail page if provided.
    /// </summary>
    public string? SponsorContactName { get; init; }

    /// <summary>
    /// Optional internal contact email address for the sponsor, intended for authenticated users only. Displayed in a separate section on the sponsor detail page if provided.
    /// </summary>
    public string? SponsorContactEmail { get; init; }

    /// <summary>
    /// Optional internal contact phone number for the sponsor, intended for authenticated users only. Displayed in a separate section on the sponsor detail page if provided.
    /// </summary>
    public string? SponsorContactPhone { get; init; }

    /// <summary>
    /// Optional internal contact phone number type for the sponsor, intended for authenticated users only. Displayed in a separate section on the sponsor detail page if provided.
    /// </summary>
    public string? SponsorContactPhoneType { get; init; }

    /// <summary>
    /// Indicates whether the sponsor has any business address information available for display.
    /// </summary>
    public bool HasAddress =>
        BusinessStreet is not null || BusinessCity is not null;

    /// <summary>
    /// Indicates whether the sponsor has any contact channels (email or phone numbers) available for display.
    /// </summary>
    public bool HasContactChannels =>
        ContactEmail is not null || PhoneNumbers.Count > 0;

    /// <summary>
    /// Indicates whether the sponsor has any social media links available for display.
    /// </summary>
    public bool HasSocialMedia =>
        FacebookUrl is not null || InstagramUrl is not null;

    /// <summary>
    /// Indicates whether the sponsor has any promotional information (promotional notes or live read script) available for display.
    /// </summary>
    public bool HasPromotionalInfo =>
        PromotionalNotes is not null || LiveReadScript is not null;

    /// <summary>
    /// Indicates whether the sponsor has any internal contact information available for display to authenticated users.
    /// </summary>
    public bool HasInternalContact =>
        SponsorContactName is not null;
}