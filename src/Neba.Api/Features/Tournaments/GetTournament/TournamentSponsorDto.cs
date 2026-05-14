namespace Neba.Api.Features.Tournaments.GetTournament;

/// <summary>
/// Represents sponsor information for a tournament, including name, branding, and related links.
/// </summary>
/// <remarks>Use this data transfer object to convey sponsor details when displaying or processing
/// tournament-related information. All required properties must be set to ensure the sponsor is properly identified and
/// attributed.</remarks>
public sealed record TournamentSponsorDto
{
    /// <summary>
    /// Gets the name of the sponsor. This is a required property and should be set to a non-empty string that accurately represents the sponsor's name for display and attribution purposes.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the URL-friendly identifier for the entity.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Gets the website URL associated with the entity.
    /// </summary>
    public Uri? WebsiteUrl { get; init; }

    /// <summary>
    /// Gets the tag phrase associated with the current instance.
    /// </summary>
    public string? TagPhrase { get; init; }

    /// <summary>
    /// Gets the URI of the logo image associated with this instance.
    /// </summary>
    public Uri? LogoUrl { get; internal set; }

    /// <summary>
    /// Gets the file system path to the logo image, if available.
    /// </summary>
    public string? LogoPath { get; init; }

    /// <summary>
    /// Gets the name or identifier of the container where the logo is stored.
    /// </summary>
    public string? LogoContainer { get; init; }
}