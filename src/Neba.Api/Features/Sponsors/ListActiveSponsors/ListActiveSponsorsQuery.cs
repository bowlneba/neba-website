using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Sponsors.ListActiveSponsors;

internal sealed record ListActiveSponsorsQuery
    : ICachedQuery<IReadOnlyCollection<SponsorSummaryDto>>
{
    public CacheDescriptor Cache
        => CacheDescriptors.Sponsors.ListActiveSponsors(CallerHasSponsorManagementPermission);

    public TimeSpan Expiry
        => TimeSpan.FromDays(30);

    public required bool CallerHasSponsorManagementPermission { get; init; }
}