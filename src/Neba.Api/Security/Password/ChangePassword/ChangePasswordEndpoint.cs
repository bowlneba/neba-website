using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.ChangePassword;

namespace Neba.Api.Security.Password.ChangePassword;

internal sealed class ChangePasswordEndpoint(Messaging.ICommandHandler<ChangePasswordCommand, ChangePasswordDto> commandHandler)
    : Endpoint<ChangePasswordRequest, ChangePasswordResponse>
{
    private readonly Messaging.ICommandHandler<ChangePasswordCommand, ChangePasswordDto> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("password/change");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(SecurityConfiguration.AuthenticatedPolicy);

        Description(description => description
            .WithName("ChangePassword")
            .WithTags("Authenticated")
            .Produces<ChangePasswordResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(ChangePasswordRequest req, CancellationToken ct)
    {
        // The user comes from the token, never the body, so a caller can only change their own password.
        if (User.CallerUserId is not { } userId)
        {
            await Send.UnauthorizedAsync(ct);
            // Stryker disable once Statement
            return;
        }

        var command = new ChangePasswordCommand
        {
            UserId = userId,
            CurrentPassword = req.CurrentPassword,
            NewPassword = req.NewPassword
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                await Send.UnauthorizedAsync(ct);
                // Stryker disable once Statement
                return;
            }

            foreach (var error in result.Errors)
                AddError(error.Description);

            await Send.ErrorsAsync(
                result.FirstError.Type == ErrorType.Conflict
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status422UnprocessableEntity,
                ct);

            // Stryker disable once Statement
            return;
        }

        var dto = result.Value;

        // Stryker disable once Statement
        await Send.OkAsync(new ChangePasswordResponse
        {
            AccessToken = dto.AccessToken,
            RefreshToken = dto.RefreshToken,
            ExpiresAt = dto.ExpiresAt,
            UserId = dto.UserId.ToString(),
            Email = dto.Email
        }, ct);
    }
}
