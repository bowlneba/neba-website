using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListOilPatterns;

internal sealed record ListOilPatternsQuery
    : ICachedQuery<IReadOnlyCollection<OilPatternSummaryDto>>
{
    public CacheDescriptor Cache
        => CacheDescriptors.OilPatterns.List;

    public TimeSpan Expiry
        => TimeSpan.FromDays(30);
}