namespace Neba.Api.Contracts.Sponsors.CreateSponsor;

/// <summary>
/// Response returned after successfully creating a sponsor.
/// </summary>
public sealed record SponsorResponse
{
    /// <summary>
    /// The ULID string that uniquely identifies the newly created sponsor.
    /// </summary>
    public required string SponsorId { get; init; }

    /// <summary>
    /// The normalized, unique slug assigned to the sponsor.
    /// </summary>
    public required string Slug { get; init; }
}