using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.Login;

namespace Neba.Api.Security.Login;

internal sealed class LoginEndpoint(Messaging.ICommandHandler<LoginCommand, LoginDto> commandHandler)
    : Endpoint<LoginRequest, LoginResponse>
{
    private readonly Messaging.ICommandHandler<LoginCommand, LoginDto> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("login");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("Login")
            .WithTags("Public")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var command = new LoginCommand { Email = req.Email, Password = req.Password };
        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsSuccess)
        {
            var dto = result.Value;

            // Stryker disable once Statement
            await Send.OkAsync(new LoginResponse
            {
                AccessToken = dto.AccessToken,
                RefreshToken = dto.RefreshToken,
                ExpiresAt = dto.ExpiresAt,
                UserId = dto.UserId.ToString(),
                Email = dto.Email,
            }, ct);

            // Stryker disable once Statement
            return;
        }

        await Send.UnauthorizedAsync(ct);
    }
}