using ErrorOr;

using Neba.Api.Caching;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Bowlers.GetBowlerTitles;

/// <summary>
/// Represents a query to retrieve the titles won by a specific bowler, including details about each tournament.
/// </summary>
public sealed record GetBowlerTitlesQuery
    : ICachedQuery<ErrorOr<BowlerTitlesDto>>
{
    /// <summary>
    /// The identifier of the bowler whose titles are being queried.
    /// </summary>
    public required BowlerId BowlerId { get; init; }

    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.Bowlers.Titles(BowlerId);

    /// <inheritdoc />
    public TimeSpan Expiry
        => TimeSpan.FromDays(7);
}
