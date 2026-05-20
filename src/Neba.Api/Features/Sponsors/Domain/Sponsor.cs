
using Neba.Api.Contacts.Domain;
using Neba.Api.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Sponsors.Domain;

/// <summary>
/// A company or individual with a formal promotional relationship with NEBA, receiving recognition
/// and visibility across NEBA events, publications, and digital properties. Aggregate root for all
/// sponsorship concepts.
/// </summary>
public sealed class Sponsor
    : AggregateRoot
{
    /// <summary>
    /// Unique identifier for the sponsor.
    /// </summary>
    public required SponsorId Id { get; init; }

    /// <summary>
    /// The display name of the sponsor — company name (e.g., "Storm Products Inc.") or individual
    /// name (e.g., "Tony &amp; Suzanne Reynaud").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// URL-friendly identifier for the sponsor.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Indicates whether the sponsor is a current sponsor.
    /// </summary>
    public required bool IsCurrentSponsor { get; init; }

    /// <summary>
    /// Priority of the sponsor.
    /// </summary>
    public required int Priority { get; init; }

    /// <summary>
    /// Tier of the sponsor.
    /// </summary>
    public required SponsorTier Tier { get; init; }

    /// <summary>
    /// Category of the sponsor.
    /// </summary>
    public required SponsorCategory Category { get; init; }

    /// <summary>
    /// Logo of the sponsor.
    /// </summary>
    public StoredFile? Logo { get; init; }

    /// <summary>
    /// Website URL of the sponsor.
    /// </summary>
    public Uri? WebsiteUrl { get; init; }

    /// <summary>
    /// Tagline or slogan of the sponsor.
    /// </summary>
    public string? TagPhrase { get; init; }

    /// <summary>
    /// Description of the sponsor.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Text to be read live about the sponsor.
    /// </summary>
    public string? LiveReadText { get; init; }

    /// <summary>
    /// Promotional notes for the sponsor.
    /// </summary>
    public string? PromotionalNotes { get; init; }

    /// <summary>
    /// Facebook URL of the sponsor.
    /// </summary>
    public Uri? FacebookUrl { get; init; }

    /// <summary>
    /// Instagram URL of the sponsor.
    /// </summary>
    public Uri? InstagramUrl { get; init; }

    /// <summary>
    /// Business address of the sponsor.
    /// </summary>
    public Address? BusinessAddress { get; init; }

    /// <summary>
    /// Business email address of the sponsor.
    /// </summary>
    public EmailAddress? BusinessEmail { get; init; }

    /// <summary>
    /// Phone numbers of the sponsor.
    /// </summary>
    public IReadOnlyCollection<PhoneNumber> PhoneNumbers { get; init; } = [];

    /// <summary>
    /// Contact information for the sponsor.
    /// </summary>
    public ContactInfo? SponsorContact { get; init; }

    internal IReadOnlyCollection<TournamentSponsor> TournamentsSponsored { get; init; } = [];
}