using System.Globalization;

using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.GetUser;
using Neba.Api.Messaging;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Security.GetUser;

internal sealed class GetUserEndpoint(IQueryHandler<GetUserQuery, ErrorOr<UserDetailDto>> queryHandler)
    : Endpoint<GetUserRequest, UserDetailResponse>
{
    private readonly IQueryHandler<GetUserQuery, ErrorOr<UserDetailDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("users/{userId}");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.GetUsers.PolicyName);

        Description(description => description
            .WithName("GetUser")
            .WithTags("Admin")
            .Produces<UserDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(GetUserRequest req, CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new GetUserQuery { UserId = Ulid.Parse(req.UserId, CultureInfo.InvariantCulture) }, ct);

        if (result.IsError)
        {
            await Send.NotFoundAsync(ct);
            // Stryker disable once Statement
            return;
        }

        var dto = result.Value;

        // Stryker disable once Statement
        await Send.OkAsync(new UserDetailResponse
        {
            UserId = dto.UserId.ToString(),
            Email = dto.Email,
            EmailConfirmed = dto.EmailConfirmed,
            Roles = dto.Roles,
            AddedPermissions = dto.AddedPermissions,
            RemovedPermissions = dto.RemovedPermissions
        }, ct);
    }
}