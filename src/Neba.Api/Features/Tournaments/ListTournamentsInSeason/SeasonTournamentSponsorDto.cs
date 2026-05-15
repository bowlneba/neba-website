namespace Neba.Api.Features.Tournaments.ListTournamentsInSeason;

/// <summary>
/// Represents a sponsor associated with a tournament, including identifying and contact information.
/// </summary>
public sealed record SeasonTournamentSponsorDto
{
    /// <summary>
    /// Gets the name associated with the current instance.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the URL-friendly identifier for the entity, which can be used in URLs or as a unique key for referencing the sponsor. This is a required property and should be set to a non-empty string that accurately represents the sponsor's identity in a concise format.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Gets the logo URL associated with the entity.
    /// </summary>
    public Uri? LogoUrl { get; internal set; }

    /// <summary>
    /// Gets the name or identifier of the container that stores the logo asset.
    /// </summary>
    public string? LogoContainer { get; init; }

    /// <summary>
    /// Gets the file system path to the logo image associated with this instance.
    /// </summary>
    public string? LogoPath { get; init; }
}
