using Neba.Api.Messaging;

namespace Neba.Api.Security.ListRoles;

internal sealed record ListRolesQuery
    : IQuery<IReadOnlyCollection<RoleDto>>;