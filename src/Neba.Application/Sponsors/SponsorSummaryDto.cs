namespace Neba.Application.Sponsors;

/// <summary>
/// Data transfer object containing summary details for a sponsor.
/// </summary>
public sealed record SponsorSummaryDto
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
    /// URI of the sponsor's logo.
    /// </summary>
    public Uri? LogoUrl { get; set; }

    /// <summary>
    /// Blob storage container name where the logo is stored.
    /// </summary>
    public string? LogoContainer { get; init; }

    /// <summary>
    /// Blob storage path to the sponsor logo.
    /// </summary>
    public string? LogoPath { get; init; }

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
    /// URL of the sponsor's website.
    /// </summary>
    public Uri? WebsiteUrl { get; init; }

    /// <summary>
    /// URL of the sponsor's Facebook profile.
    /// </summary>
    public Uri? FacebookUrl { get; init; }

    /// <summary>
    /// URL of the sponsor's Instagram profile.
    /// </summary>
    public Uri? InstagramUrl { get; init; }
}