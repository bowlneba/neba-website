
using System.Diagnostics.CodeAnalysis;

using ErrorOr;

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

    private const string ReservedSlugNew = "new";

    /// <summary>
    /// Creates a new sponsor. If <paramref name="slug"/> is null or empty, the slug is generated from
    /// <paramref name="name"/>. Returns a validation error if <paramref name="name"/> is empty, the
    /// normalized slug has no alphanumeric characters, or the normalized slug is the reserved value
    /// "new" (reserved for the <c>/sponsors/new</c> create route). <paramref name="id"/> is
    /// production-optional — it exists so test factories can assign a deterministic ID for stable
    /// Verify snapshots; production callers always omit it.
    /// </summary>
    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Aggregate factory method — each parameter is a required or optional field of the always-valid Sponsor invariant (see CLAUDE.md 'Always-Valid Entities'); splitting into a parameter object would just move the same fields into a second type with no behavior of its own.")]
    public static ErrorOr<Sponsor> Create(
        string name,
        bool isCurrentSponsor,
        int priority,
        SponsorTier tier,
        SponsorCategory category,
        string? slug = null,
        StoredFile? logo = null,
        Uri? websiteUrl = null,
        string? tagPhrase = null,
        string? description = null,
        string? liveReadText = null,
        string? promotionalNotes = null,
        Uri? facebookUrl = null,
        Uri? instagramUrl = null,
        Address? businessAddress = null,
        EmailAddress? businessEmail = null,
        IReadOnlyCollection<PhoneNumber>? phoneNumbers = null,
        ContactInfo? sponsorContact = null,
        SponsorId? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return SponsorErrors.NameRequired;
        }

        var normalizedSlug = SlugNormalizer.Normalize(string.IsNullOrEmpty(slug)
            ? name
            : slug);

        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return SponsorErrors.SlugInvalid;
        }

        if (normalizedSlug == ReservedSlugNew)
        {
            return SponsorErrors.SlugReserved;
        }

        return new Sponsor
        {
            Id = id ?? SponsorId.New(),
            Name = name,
            Slug = normalizedSlug,
            IsCurrentSponsor = isCurrentSponsor,
            Priority = priority,
            Tier = tier,
            Category = category,
            Logo = logo,
            WebsiteUrl = websiteUrl,
            TagPhrase = tagPhrase,
            Description = description,
            LiveReadText = liveReadText,
            PromotionalNotes = promotionalNotes,
            FacebookUrl = facebookUrl,
            InstagramUrl = instagramUrl,
            BusinessAddress = businessAddress,
            BusinessEmail = businessEmail,
            PhoneNumbers = phoneNumbers ?? [],
            SponsorContact = sponsorContact
        };
    }
}