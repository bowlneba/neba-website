using Neba.Application.Caching;
using Neba.Application.Messaging;

namespace Neba.Application.Sponsors.ListActiveSponsors;

/// <summary>
/// Query to retrieve a list of all active sponsors with summary information, intended for caching.
/// </summary>
public sealed record ListActiveSponsorsQuery
    : ICachedQuery<IReadOnlyCollection<SponsorSummaryDto>>
{
    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.Sponsors.ListActiveSponsors;

    /// <inheritdoc />
    public TimeSpan Expiry
        => TimeSpan.FromDays(30);
}