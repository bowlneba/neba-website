using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Security.ListUsers;
using Neba.Api.Messaging;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Security.ListUsers;

internal sealed class ListUsersEndpoint(IQueryHandler<ListUsersQuery, IReadOnlyCollection<UserSummaryDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<UserSummaryResponse>>
{
    private readonly IQueryHandler<ListUsersQuery, IReadOnlyCollection<UserSummaryDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("users");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.ResetUserPassword.PolicyName);

        Description(description => description
            .WithName("ListUsers")
            .WithTags("Admin")
            .Produces<CollectionResponse<UserSummaryResponse>>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListUsersQuery(), ct);

        var response = new CollectionResponse<UserSummaryResponse>
        {
            Items = [.. result.Select(user => new UserSummaryResponse
            {
                UserId = user.UserId.ToString(),
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                Roles = user.Roles
            })]
        };

        await Send.OkAsync(response, ct);
    }
}