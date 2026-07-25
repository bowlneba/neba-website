using Neba.Api.Contacts;
using Neba.Api.Features.Sponsors.Domain;

namespace Neba.Api.Features.Sponsors.GetSponsorDetail;

/// <summary>
/// Application-layer projection of a sponsor's detail data, passed from the handler to the endpoint.
/// </summary>
public sealed record SponsorDetailDto
{
    /// <summary>
    /// Unique identifier for the sponsor.
    /// </summary>
    public required SponsorId Id { get; init; }

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
    public required string Tier { get; init; }

    /// <summary>
    /// Sponsor category.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// URL of the sponsor's logo; null if no logo is available.
    /// </summary>
    public Uri? LogoUrl { get; init; }

    /// <summary>
    /// The Azure Blob Storage container the logo is stored in. Populated only when the caller has
    /// sponsor management permission (used to resubmit the logo unchanged when editing the sponsor);
    /// null for anonymous callers or when there is no logo.
    /// </summary>
    public string? LogoContainer { get; init; }

    /// <summary>
    /// The blob path of the logo. Populated only when the caller has sponsor management permission;
    /// null for anonymous callers or when there is no logo.
    /// </summary>
    public string? LogoPath { get; init; }

    /// <summary>
    /// The MIME content type of the logo. Populated only when the caller has sponsor management
    /// permission; null for anonymous callers or when there is no logo.
    /// </summary>
    public string? LogoContentType { get; init; }

    /// <summary>
    /// The logo's file size in bytes. Populated only when the caller has sponsor management permission;
    /// null for anonymous callers or when there is no logo.
    /// </summary>
    public long? LogoSizeInBytes { get; init; }

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
    /// Text to be read live about the sponsor. Only populated for callers with sponsor-management permission.
    /// </summary>
    public string? LiveReadText { get; init; }

    /// <summary>
    /// Internal promotional notes for staff. Only populated for callers with sponsor-management permission.
    /// </summary>
    public string? PromotionalNotes { get; init; }

    /// <summary>
    /// URL to the sponsor's Facebook profile.
    /// </summary>
    public Uri? FacebookUrl { get; init; }

    /// <summary>
    /// URL to the sponsor's Instagram profile.
    /// </summary>
    public Uri? InstagramUrl { get; init; }

    /// <summary>
    /// Business address of the sponsor.
    /// </summary>
    public AddressDto? BusinessAddress { get; init; }

    /// <summary>
    /// Business email address for sponsor inquiries.
    /// </summary>
    public string? BusinessEmailAddress { get; init; }

    /// <summary>
    /// Contact phone numbers associated with the sponsor.
    /// </summary>
    public IReadOnlyCollection<PhoneNumberDto> PhoneNumbers { get; init; } = [];

    /// <summary>
    /// The sponsor's internal contact person. Only populated for callers with sponsor-management permission.
    /// </summary>
    public SponsorContactDto? Contact { get; init; }

    /// <summary>
    /// Tournaments sponsored by this sponsor, most recent first.
    /// </summary>
    public IReadOnlyCollection<SponsorDetailTournamentDto> TournamentsSponsored { get; init; } = [];
}