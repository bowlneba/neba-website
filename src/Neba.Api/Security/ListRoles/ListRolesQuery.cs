using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Security.ListRoles;

/// <summary>
/// Lists roles and the permissions each grants. Role grants change only when the seeder runs at
/// startup, so the result is cached for an hour.
/// </summary>
internal sealed record ListRolesQuery
    : ICachedQuery<IReadOnlyCollection<RoleDto>>
{
    public CacheDescriptor Cache
        => CacheDescriptors.Security.ListRoles;

    public TimeSpan Expiry
        => TimeSpan.FromHours(1);
}
