using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Seasons.ListSeasons;

internal sealed record ListSeasonsQuery
    : ICachedQuery<IReadOnlyCollection<SeasonDto>>
{
    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.Seasons.List;

    /// <inheritdoc />
    public TimeSpan Expiry
        => TimeSpan.FromDays(90);
}