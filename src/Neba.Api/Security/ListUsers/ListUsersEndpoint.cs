using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Security.ListUsers;
using Neba.Api.Messaging;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Security.ListUsers;

internal sealed class ListUsersEndpoint(IQueryHandler<ListUsersQuery, PagedResult<UserSummaryDto>> queryHandler)
    : Endpoint<ListUsersRequest, PaginationResponse<UserSummaryResponse>>
{
    private readonly IQueryHandler<ListUsersQuery, PagedResult<UserSummaryDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("users");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.GetUsers.PolicyName);

        Description(description => description
            .WithName("ListUsers")
            .WithTags("Admin")
            .Produces<PaginationResponse<UserSummaryResponse>>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden));
    }

    public override async Task HandleAsync(ListUsersRequest req, CancellationToken ct)
    {
        var query = new ListUsersQuery
        {
            Page = req.Page,
            PageSize = req.PageSize
        };

        var result = await _queryHandler.HandleAsync(query, ct);

        var response = new PaginationResponse<UserSummaryResponse>
        {
            Items = [.. result.Items.Select(user => new UserSummaryResponse
            {
                UserId = user.UserId.ToString(),
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                Roles = user.Roles
            })],
            TotalItems = result.TotalItems,
            PageNumber = req.Page,
            PageSize = req.PageSize
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}