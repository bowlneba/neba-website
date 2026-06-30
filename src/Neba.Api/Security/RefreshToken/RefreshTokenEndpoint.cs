using System.Globalization;

using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.RefreshToken;

namespace Neba.Api.Security.RefreshToken;

internal sealed class RefreshTokenEndpoint(Messaging.ICommandHandler<RefreshTokenCommand, RefreshTokenDto> commandHandler)
    : Endpoint<RefreshTokenRequest, RefreshTokenResponse>
{
    private readonly Messaging.ICommandHandler<RefreshTokenCommand, RefreshTokenDto> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("refresh");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("RefreshToken")
            .WithTags("Public")
            .Produces<RefreshTokenResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        var command = new RefreshTokenCommand
        {
            UserId = Ulid.Parse(req.UserId, CultureInfo.InvariantCulture),
            RefreshToken = req.RefreshToken,
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsSuccess)
        {
            var dto = result.Value;

            // Stryker disable once Statement
            await Send.OkAsync(new RefreshTokenResponse
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