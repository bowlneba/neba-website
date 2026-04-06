namespace Neba.Api.Contracts.Sponsors;

/// <summary>
/// Response model containing summary details for a sponsor, intended for API responses.
/// </summary>
public sealed record SponsorSummaryResponse
{
    /// <summary>
    /// Display name of the sponsor.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// URL-friendly identifier for the sponsor.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// URL to the sponsor's logo image.
    /// </summary>
    public Uri? LogoUrl { get; init; }

    /// <summary>
    /// Indicates whether the sponsor is currently active.
    /// </summary>
    public required bool IsCurrentSponsor { get; init; }

    /// <summary>
    /// Priority of the sponsor.
    /// </summary>
    public required int Priority { get; init; }

    /// <summary>
    /// Tier of the sponsor.
    /// </summary>
    public required string Tier { get; init; }

    /// <summary>
    /// Category of the sponsor.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Tagline or phrase associated with the sponsor.
    /// </summary>
    public string? TagPhrase { get; init; }

    /// <summary>
    /// Description of the sponsor.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// URL to the sponsor's website.
    /// </summary>
    public Uri? WebsiteUrl { get; init; }

    /// <summary>
    /// URL to the sponsor's Facebook page.
    /// </summary>
    public Uri? FacebookUrl { get; init; }

    /// <summary>
    /// URL to the sponsor's Instagram page.
    /// </summary>
    public Uri? InstagramUrl { get; init; }
}