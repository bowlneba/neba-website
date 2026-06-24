using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.Register;

namespace Neba.Api.Security.Register;

internal sealed class RegisterEndpoint(Messaging.ICommandHandler<RegisterCommand, Ulid> commandHandler)
    : Endpoint<RegisterRequest, RegisterResponse>
{
    private readonly Messaging.ICommandHandler<RegisterCommand, Ulid> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("register");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("Register")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var command = new RegisterCommand { Email = req.Email, Password = req.Password };
        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsSuccess)
        {
            // Stryker disable once Statement
            await Send.CreatedAtAsync("GetCurrentUser", routeValues: null, responseBody: new RegisterResponse { UserId = result.Value.ToString() }, cancellation: ct);

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
