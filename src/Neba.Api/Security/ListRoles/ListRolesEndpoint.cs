using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.ListRoles;
using Neba.Api.Messaging;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Security.ListRoles;

internal sealed class ListRolesEndpoint(IQueryHandler<ListRolesQuery, IReadOnlyCollection<RoleDto>> queryHandler)
    : EndpointWithoutRequest<ListRolesResponse>
{
    private readonly IQueryHandler<ListRolesQuery, IReadOnlyCollection<RoleDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("roles");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.GetUsers.PolicyName);

        Description(description => description
            .WithName("ListRoles")
            .WithTags("Admin")
            .Produces<ListRolesResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var roles = await _queryHandler.HandleAsync(new ListRolesQuery(), ct);

        // Stryker disable once Statement
        await Send.OkAsync(new ListRolesResponse
        {
            Roles = [.. roles.Select(role => new RoleResponse { Name = role.Name, Permissions = role.Permissions })]
        }, ct);
    }
}
