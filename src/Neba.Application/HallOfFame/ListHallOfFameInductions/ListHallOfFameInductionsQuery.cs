using Neba.Application.Caching;
using Neba.Application.Messaging;

namespace Neba.Application.HallOfFame.ListHallOfFameInductions;

/// <summary>
/// Query to retrieve a list of Hall of Fame inductions.
/// </summary>
public sealed record ListHallOfFameInductionsQuery
    : ICachedQuery<IReadOnlyCollection<HallOfFameInductionDto>>
{
    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.HallOfFame.ListInductions;

    /// <inheritdoc />
    public TimeSpan Expiry
        => TimeSpan.FromDays(100);
}