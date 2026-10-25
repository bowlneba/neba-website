using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.ForgotPassword;
using Neba.Api.RateLimiting;

namespace Neba.Api.Security.Password.ForgotPassword;

internal sealed class ForgotPasswordEndpoint(Messaging.ICommandHandler<ForgotPasswordCommand> commandHandler)
    : Endpoint<ForgotPasswordRequest>
{
    private readonly Messaging.ICommandHandler<ForgotPasswordCommand> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("password/forgot");
        Group<SecurityEndpointGroup>();

        // The group applies the "public" limit; the endpoint's own, stricter policy is the one that
        // takes effect (covered by an endpoint test).
        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0))
            .RequireRateLimiting(RateLimitingConfiguration.PasswordRecoveryPolicy));

        AllowAnonymous();

        Description(description => description
            .WithName("ForgotPassword")
            .WithTags("Public")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status429TooManyRequests));
    }

    public override async Task HandleAsync(ForgotPasswordRequest req, CancellationToken ct)
    {
        var command = new ForgotPasswordCommand
        {
            Email = req.Email,
            RequesterIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
        };

        // The handler never returns an error; the response must be identical for every email.
        _ = await _commandHandler.HandleAsync(command, ct);

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}