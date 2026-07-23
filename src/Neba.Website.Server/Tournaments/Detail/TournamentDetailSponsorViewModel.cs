namespace Neba.Website.Server.Tournaments.Detail;

/// <summary>
/// Sponsor summary for display on the tournament detail page.
/// </summary>
public sealed record TournamentDetailSponsorViewModel
{
    /// <summary>
    /// The ULID string identifying the sponsor.
    /// </summary>
    public required string SponsorId { get; init; }

    /// <summary>
    /// Display name of the sponsor.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// URL-friendly slug for linking to the sponsor detail page.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// URL to the sponsor's logo image; null if not available.
    /// </summary>
    public Uri? LogoUrl { get; init; }

    /// <summary>
    /// Sponsor's website URL; null if not available.
    /// </summary>
    public Uri? WebsiteUrl { get; init; }

    /// <summary>
    /// Short tagline for the sponsor; null if not set.
    /// </summary>
    public string? TagPhrase { get; init; }

    /// <summary>
    /// Whether this sponsor is the tournament's title sponsor.
    /// </summary>
    public required bool TitleSponsor { get; init; }

    /// <summary>
    /// The sponsorship amount, in dollars.
    /// </summary>
    public required decimal SponsorshipAmount { get; init; }
}