using Neba.Application.Caching;
using Neba.Application.Messaging;

namespace Neba.Application.Sponsors.GetSponsorDetail;

/// <summary>
/// A query to retrieve sponsor detail information by slug.
/// </summary>
public sealed record GetSponsorDetailQuery
    : ICachedQuery<SponsorDetailDto?>
{
    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.Sponsors.Detail(Slug);

    /// <inheritdoc />
    public TimeSpan Expiry
        => TimeSpan.FromDays(30);

    /// <summary>
    /// The sponsor slug used to identify the sponsor.
    /// </summary>
    public required string Slug { get; init; }
}