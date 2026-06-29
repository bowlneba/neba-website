using System.Globalization;

using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.ResetPassword;

using SecurityRoles = Neba.Api.Security.Domain.Roles;

namespace Neba.Api.Security.Password.ResetPassword;

internal sealed class ResetPasswordEndpoint(Messaging.ICommandHandler<ResetPasswordCommand> commandHandler)
    : Endpoint<ResetPasswordRequest>
{
    private readonly Messaging.ICommandHandler<ResetPasswordCommand> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("password/reset");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0))
            .RequireAuthorization());

        Roles(SecurityRoles.Admin);

        Description(description => description
            .WithName("ResetPassword")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        var command = new ResetPasswordCommand { UserId = Ulid.Parse(req.UserId, CultureInfo.InvariantCulture) };
        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                await Send.NotFoundAsync(ct);
                // Stryker disable once Statement
                return;
            }

            foreach (var error in result.Errors)
                AddError(error.Description);

            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}