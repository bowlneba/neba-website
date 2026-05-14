namespace Neba.Api.Features.Sponsors.ListActiveSponsors;

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
    public Uri? LogoUrl { get; init; }

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
    public bool IsCurrentSponsor { get; init; }

    /// <summary>
    /// Priority of the sponsor.
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    /// Tier of the sponsor.
    /// </summary>
    public string Tier { get; init; } = string.Empty;

    /// <summary>
    /// Category of the sponsor.
    /// </summary>
    public string Category { get; init; } = string.Empty;

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