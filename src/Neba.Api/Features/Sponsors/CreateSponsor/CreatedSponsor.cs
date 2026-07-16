using Neba.Api.Features.Sponsors.Domain;

namespace Neba.Api.Features.Sponsors.CreateSponsor;

/// <summary>
/// Result of successfully creating a sponsor, including its identifier and normalized slug.
/// </summary>
public sealed record CreatedSponsor
{
    /// <summary>
    /// The unique identifier of the newly created sponsor.
    /// </summary>
    public required SponsorId Id { get; init; }

    /// <summary>
    /// The normalized slug assigned to the newly created sponsor.
    /// </summary>
    public required string Slug { get; init; }
}