using Neba.Domain.Contact;
using Neba.Domain.Storage;
using Neba.Domain.Tournaments;

namespace Neba.Domain.Sponsors;

/// <summary>
/// Represents a sponsor that supports the NEBA organization. Sponsors can be categorized by their tier and category, and can have various contact and promotional information associated with them.
/// </summary>
public sealed class Sponsor
    : AggregateRoot
{
    /// <summary>
    /// Unique identifier for the sponsor.
    /// </summary>
    public required SponsorId Id { get; init; }

    /// <summary>
    /// Name of the sponsor.
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