using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.CreateUser;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Security.CreateUser;

internal sealed class CreateUserEndpoint(Messaging.ICommandHandler<CreateUserCommand, Ulid> commandHandler)
    : Endpoint<CreateUserRequest, CreateUserResponse>
{
    private readonly Messaging.ICommandHandler<CreateUserCommand, Ulid> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("users");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CreateUser.PolicyName);

        Description(description => description
            .WithName("CreateUser")
            .WithTags("Security")
            .Produces<CreateUserResponse>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        var command = new CreateUserCommand
        {
            Email = req.User.Email,
            Roles = req.User.Roles,
            UsbcId = req.User.UsbcId,
            PhoneNumber = req.User.PhoneNumber,
            Claims = [.. req.User.Claims.Select(c => (c.Type, c.Value))]
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsSuccess)
        {
            // Stryker disable once Statement
            await Send.CreatedAtAsync("GetCurrentUser", routeValues: null, responseBody: new CreateUserResponse { UserId = result.Value.ToString() }, cancellation: ct);

            // Stryker disable once Statement
            return;
        }

        if (result.FirstError.Type == ErrorType.Conflict)
        {
            AddError(result.FirstError.Description);
            await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);

            // Stryker disable once Statement
            return;
        }

        foreach (var error in result.Errors)
            AddError(error.Description);

        await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
    }
}