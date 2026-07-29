using System.Globalization;

using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.SetPasswordFromToken;

namespace Neba.Api.Security.Password.SetPasswordFromToken;

internal sealed class SetPasswordFromTokenEndpoint(Messaging.ICommandHandler<SetPasswordFromTokenCommand> commandHandler)
    : Endpoint<SetPasswordFromTokenRequest>
{
    private readonly Messaging.ICommandHandler<SetPasswordFromTokenCommand> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("password/set-from-token");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("SetPasswordFromToken")
            .WithTags("Public")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(SetPasswordFromTokenRequest req, CancellationToken ct)
    {
        var command = new SetPasswordFromTokenCommand
        {
            UserId = Ulid.Parse(req.UserId, CultureInfo.InvariantCulture),
            Token = req.Token,
            NewPassword = req.NewPassword
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
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