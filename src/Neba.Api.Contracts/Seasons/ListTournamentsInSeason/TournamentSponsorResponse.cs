namespace Neba.Api.Contracts.Seasons.ListTournamentsInSeason;

/// <summary>
/// Simplified sponsor details included in a tournament summary response.
/// </summary>
public sealed record TournamentSponsorResponse
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
}
