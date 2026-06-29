using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Security.Logout;

internal sealed class LogoutEndpoint(Messaging.ICommandHandler<LogoutCommand> commandHandler)
    : EndpointWithoutRequest
{
    private readonly Messaging.ICommandHandler<LogoutCommand> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("logout");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0))
            .RequireAuthorization());

        Description(description => description
            .WithName("Logout")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdString = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdString is not null && Ulid.TryParse(userIdString, out var userId))
        {
            var command = new LogoutCommand { UserId = userId };
            await _commandHandler.HandleAsync(command, ct);
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}