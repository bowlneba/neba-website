using ErrorOr;

using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Sponsors.GetSponsorDetail;

/// <summary>
/// A query to retrieve sponsor detail information by slug.
/// </summary>
public sealed record GetSponsorDetailQuery
    : ICachedQuery<ErrorOr<SponsorDetailDto>>
{
    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.Sponsors.Detail(Slug, CallerHasSponsorManagementPermission);

    /// <inheritdoc />
    public TimeSpan Expiry
        => TimeSpan.FromDays(30);

    /// <summary>
    /// The sponsor slug used to identify the sponsor.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Whether the caller holds a sponsor-management permission — permits viewing a sponsor that
    /// isn't the current/active one, which is otherwise treated as not found.
    /// </summary>
    public required bool CallerHasSponsorManagementPermission { get; init; }
}