using System.Globalization;

using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.UpdateUser;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Security.UpdateUser;

internal sealed class UpdateUserEndpoint(Messaging.ICommandHandler<UpdateUserCommand, Updated> commandHandler)
    : Endpoint<UpdateUserRequest>
{
    private readonly Messaging.ICommandHandler<UpdateUserCommand, Updated> _commandHandler = commandHandler;

    public override void Configure()
    {
        Put("users/{userId}");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditUser.PolicyName);

        Description(description => description
            .WithName("UpdateUser")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(UpdateUserRequest req, CancellationToken ct)
    {
        if (User.CallerUserId is not { } callerUserId)
        {
            await Send.UnauthorizedAsync(ct);

            // Stryker disable once Statement
            return;
        }

        var command = new UpdateUserCommand
        {
            UserId = Ulid.Parse(req.UserId, CultureInfo.InvariantCulture),
            AddedPermissions = req.User.AddedPermissions,
            RemovedPermissions = req.User.RemovedPermissions,
            CallerUserId = callerUserId,
            CallerPermissions = User.CallerPermissions
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            switch (result.FirstError.Type)
            {
                case ErrorType.NotFound:
                    await Send.NotFoundAsync(ct);
                    break;

                case ErrorType.Forbidden:
                    await Send.ForbiddenAsync(ct);
                    break;

                case ErrorType.Conflict:
                    AddError(result.FirstError.Description);
                    await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                    break;

                default:
                    foreach (var error in result.Errors)
                        AddError(error.Description);

                    await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
                    break;
            }

            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}