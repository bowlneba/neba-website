using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.GetCurrentUser;
using Neba.Api.Messaging;

namespace Neba.Api.Security.GetCurrentUser;

internal sealed class GetCurrentUserEndpoint(IQueryHandler<GetCurrentUserQuery, ErrorOr<UserDto>> queryHandler)
    : EndpointWithoutRequest<MeResponse>
{
    private readonly IQueryHandler<GetCurrentUserQuery, ErrorOr<UserDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("me");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("security")
            .MapToApiVersion(new ApiVersion(1, 0))
            .RequireAuthorization());

        Description(description => description
            .WithName("GetCurrentUser")
            .Produces<MeResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdString = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Ulid.TryParse(userIdString, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            // Stryker disable once Statement
            return;
        }

        var result = await _queryHandler.HandleAsync(new GetCurrentUserQuery { UserId = userId }, ct);

        if (result.IsError)
        {
            await Send.NotFoundAsync(ct);
            // Stryker disable once Statement
            return;
        }

        var dto = result.Value;

        // Stryker disable once Statement
        await Send.OkAsync(new MeResponse
        {
            UserId = dto.UserId.ToString(),
            Email = dto.Email,
            Roles = dto.Roles,
            UsbcId = dto.UsbcId,
        }, ct);
    }
}
