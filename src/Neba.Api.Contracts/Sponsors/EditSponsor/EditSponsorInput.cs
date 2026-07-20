using Neba.Api.Contracts.Sponsors.CreateSponsor;

namespace Neba.Api.Contracts.Sponsors.EditSponsor;

/// <summary>
/// The fields required to edit an existing sponsor. Identical to <see cref="SponsorInput"/> minus
/// <c>Slug</c>, which is immutable after creation.
/// </summary>
public sealed record EditSponsorInput
{
    /// <summary>
    /// Display name of the sponsor.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Indicates whether the sponsor is currently active.
    /// </summary>
    public required bool IsCurrentSponsor { get; init; }

    /// <summary>
    /// Priority of the sponsor.
    /// </summary>
    public required int Priority { get; init; }

    /// <summary>
    /// The sponsor tier name (see <c>SponsorTier</c>): "Title Sponsor", "Premier", or "Standard".
    /// </summary>
    public required string Tier { get; init; }

    /// <summary>
    /// The sponsor category name (see <c>SponsorCategory</c>).
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// The sponsor's logo image, already uploaded to storage.
    /// </summary>
    public SponsorLogoInput? Logo { get; init; }

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
    /// The US state postal abbreviation (e.g. "MA" — see <c>UsState</c>).
    /// </summary>
    public string? BusinessState { get; init; }

    /// <summary>
    /// Postal code for the sponsor's business address.
    /// </summary>
    public string? BusinessPostalCode { get; init; }

    /// <summary>
    /// Business email address for sponsor inquiries.
    /// </summary>
    public string? BusinessEmailAddress { get; init; }

    /// <summary>
    /// Phone numbers for the sponsor.
    /// </summary>
    public IReadOnlyCollection<SponsorPhoneNumberInput> PhoneNumbers { get; init; } = [];

    /// <summary>
    /// Contact person details for the sponsor.
    /// </summary>
    public SponsorContactInput? Contact { get; init; }
}