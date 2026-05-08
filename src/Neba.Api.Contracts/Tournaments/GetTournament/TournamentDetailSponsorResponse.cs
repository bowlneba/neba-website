namespace Neba.Api.Contracts.Tournaments.GetTournament;

/// <summary>
/// Sponsor details included in a tournament detail response.
/// </summary>
public sealed record TournamentDetailSponsorResponse
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
    /// URL to the sponsor's logo image; null if not available.
    /// </summary>
    public Uri? LogoUrl { get; init; }

    /// <summary>
    /// Sponsor's website URL; null if not available.
    /// </summary>
    public Uri? WebsiteUrl { get; init; }

    /// <summary>
    /// Short tagline or phrase for the sponsor; null if not set.
    /// </summary>
    public string? TagPhrase { get; init; }
}