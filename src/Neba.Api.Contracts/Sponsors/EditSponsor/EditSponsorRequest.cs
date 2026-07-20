namespace Neba.Api.Contracts.Sponsors.EditSponsor;

/// <summary>
/// Edits an existing sponsor.
/// </summary>
public sealed record EditSponsorRequest
{
    /// <summary>
    /// The ULID string identifying the sponsor to edit.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The sponsor fields to update.
    /// </summary>
    public required EditSponsorInput Sponsor { get; init; }
}
