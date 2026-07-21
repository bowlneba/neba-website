namespace Neba.Api.Contracts.Sponsors.CreateSponsor;

/// <summary>
/// Creates a sponsor.
/// </summary>
public sealed record CreateSponsorRequest
{
    /// <summary>
    /// The sponsor fields to create.
    /// </summary>
    public required SponsorInput Sponsor { get; init; }
}