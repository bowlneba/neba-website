
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
    /// name (e.g., "Joe &amp; Jane Smith").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier for the sponsor.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Indicates whether the sponsor is a current sponsor.
    /// </summary>
    public bool IsCurrentSponsor { get; private set; }

    /// <summary>
    /// Priority of the sponsor.
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Tier of the sponsor.
    /// </summary>
    public SponsorTier Tier { get; private set; } = SponsorTier.Standard;

    /// <summary>
    /// Category of the sponsor.
    /// </summary>
    public SponsorCategory Category { get; private set; } = SponsorCategory.Other;

    /// <summary>
    /// Logo of the sponsor.
    /// </summary>
    public StoredFile? Logo { get; private set; }

    /// <summary>
    /// Website URL of the sponsor.
    /// </summary>
    public Uri? WebsiteUrl { get; private set; }

    /// <summary>
    /// Tagline or slogan of the sponsor.
    /// </summary>
    public string? TagPhrase { get; private set; }

    /// <summary>
    /// Description of the sponsor.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Text to be read live about the sponsor.
    /// </summary>
    public string? LiveReadText { get; private set; }

    /// <summary>
    /// Promotional notes for the sponsor.
    /// </summary>
    public string? PromotionalNotes { get; private set; }

    /// <summary>
    /// Facebook URL of the sponsor.
    /// </summary>
    public Uri? FacebookUrl { get; private set; }

    /// <summary>
    /// Instagram URL of the sponsor.
    /// </summary>
    public Uri? InstagramUrl { get; private set; }

    /// <summary>
    /// Business address of the sponsor.
    /// </summary>
    public Address? BusinessAddress { get; private set; }

    /// <summary>
    /// Business email address of the sponsor.
    /// </summary>
    public EmailAddress? BusinessEmail { get; private set; }

    /// <summary>
    /// Phone numbers of the sponsor.
    /// </summary>
    // Must stay `new List<PhoneNumber>()`, NOT the `[]` collection-expression form: target-typed to
    // the interface property type, `[]` resolves to a fixed-size T[] at runtime, which throws
    // NotSupportedException when EF's owned-collection fixup tries to Add into it while
    // materializing a Sponsor with phone numbers. See CLAUDE.md "EF Core Navigation Fixup".
    // Guarded by SponsorTests.PhoneNumbers_DefaultInstance_ShouldSupportAdd_ForEfFixup.
    [SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "See preceding comment — [] would regress to a fixed-size array.")]
    [SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "See preceding comment — [] would regress to a fixed-size array.")]
    public IReadOnlyCollection<PhoneNumber> PhoneNumbers { get; private set; } = new List<PhoneNumber>();

    /// <summary>
    /// Contact information for the sponsor.
    /// </summary>
    public ContactInfo? SponsorContact { get; private set; }

    // Same fixed-size-array hazard as PhoneNumbers above — keep as new List<TournamentSponsor>().
    [SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "See PhoneNumbers comment above — [] would regress to a fixed-size array.")]
    [SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "See PhoneNumbers comment above — [] would regress to a fixed-size array.")]
    internal IReadOnlyCollection<TournamentSponsor> TournamentsSponsored { get; private set; } = new List<TournamentSponsor>();

    private const string ReservedSlugNew = "new";

    /// <summary>
    /// Creates a new sponsor. If <paramref name="slug"/> is null or empty, the slug is generated
    /// from <paramref name="name"/>. Returns a validation error if <paramref name="name"/> is empty,
    /// the normalized slug has no alphanumeric characters, or the normalized slug is the reserved
    /// value "new" (reserved for the <c>/sponsors/new</c> create route). Returns a conflict error if
    /// <paramref name="tier"/> is <see cref="SponsorTier.TitleSponsor"/> and
    /// <paramref name="isTitleSponsorshipAvailable"/> is <c>false</c> — only one sponsor may hold the
    /// Title tier at a time. <paramref name="isTitleSponsorshipAvailable"/> defaults to <c>false</c> as
    /// a fail-safe: a caller that omits it gets the safe, blocking behavior instead of an unintended
    /// bypass. It is a plain <c>bool</c> rather than a call to <c>ITitleSponsorPolicy</c> so the
    /// invariant is exercised with no mocking. This is a fast, user-friendly pre-check only — the
    /// actual guarantee against two concurrent Title-tier creates is a filtered unique database index
    /// (see <c>SponsorConfiguration</c>), which the caller must also handle by catching
    /// <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/> on save.
    /// </summary>
    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Aggregate factory method — each parameter is a required or optional field of the always-valid Sponsor invariant (see CLAUDE.md 'Always-Valid Entities'); splitting into a parameter object would just move the same fields into a second type with no behavior of its own.")]
    [SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "The `phoneNumbers ?? new List<PhoneNumber>()` fallback below must not become `?? []` — see the PhoneNumbers property comment for why.")]
    [SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "The `phoneNumbers ?? new List<PhoneNumber>()` fallback below must not become `?? []` — see the PhoneNumbers property comment for why.")]
    public static ErrorOr<Sponsor> Create(
        string name,
        bool isCurrentSponsor,
        int priority,
        SponsorTier tier,
        SponsorCategory category,
        bool isTitleSponsorshipAvailable = false,
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
        ContactInfo? sponsorContact = null)
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

        if (tier == SponsorTier.TitleSponsor && !isTitleSponsorshipAvailable)
        {
            return SponsorErrors.TitleSponsorshipUnavailable;
        }

        return new Sponsor
        {
            Id = SponsorId.New(),
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
            PhoneNumbers = phoneNumbers ?? new List<PhoneNumber>(),
            SponsorContact = sponsorContact
        };
    }

    /// <summary>
    /// Updates the sponsor's properties. Returns a validation error if <paramref name="name"/> is empty
    /// </summary>
    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
        Justification = "Aggregate mutator — mirrors Create's parameter set minus Slug (immutable). See CLAUDE.md 'Always-Valid Entities'.")]
    public ErrorOr<Updated> Update(
        string name,
        bool isCurrentSponsor,
        int priority,
        SponsorTier tier,
        SponsorCategory category,
        bool isTitleSponsorshipAvailable,
        StoredFile? logo,
        Uri? websiteUrl,
        string? tagPhrase,
        string? description,
        string? liveReadText,
        string? promotionalNotes,
        Uri? facebookUrl,
        Uri? instagramUrl,
        Address? businessAddress,
        EmailAddress? businessEmail,
        IReadOnlyCollection<PhoneNumber> phoneNumbers,
        ContactInfo? sponsorContact)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return SponsorErrors.NameRequired;
        }

        if (tier == SponsorTier.TitleSponsor && !isTitleSponsorshipAvailable)
        {
            return SponsorErrors.TitleSponsorshipUnavailable;
        }

        Name = name;
        IsCurrentSponsor = isCurrentSponsor;
        Priority = priority;
        Tier = tier;
        Category = category;
        Logo = logo;
        WebsiteUrl = websiteUrl;
        TagPhrase = tagPhrase;
        Description = description;
        LiveReadText = liveReadText;
        PromotionalNotes = promotionalNotes;
        FacebookUrl = facebookUrl;
        InstagramUrl = instagramUrl;
        BusinessAddress = businessAddress;
        BusinessEmail = businessEmail;
        PhoneNumbers = phoneNumbers;
        SponsorContact = sponsorContact;

        return Result.Updated;
    }
}