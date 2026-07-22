using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.OilPatterns.CreateOilPattern;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.CreateOilPattern;

internal sealed class CreateOilPatternEndpoint(Messaging.ICommandHandler<CreateOilPatternCommand, CreatedOilPattern> commandHandler)
    : Endpoint<CreateOilPatternRequest, CreatedOilPatternResponse>
{
    private readonly Messaging.ICommandHandler<CreateOilPatternCommand, CreatedOilPattern> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post(string.Empty);
        Group<OilPatternsEndpointGroup>();

        Options(options => options
            .WithVersionSet("OilPatterns")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CreateTournament.PolicyName);

        Description(description => description
            .WithName("CreateOilPattern")
            .WithTags("Admin")
            .Produces<CreatedOilPatternResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CreateOilPatternRequest req, CancellationToken ct)
    {
        var command = new CreateOilPatternCommand
        {
            Name = req.Name,
            Length = req.Length,
            Volume = req.Volume,
            LeftRatio = req.LeftRatio,
            RightRatio = req.RightRatio,
            KegelId = req.KegelId
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.Conflict)
            {
                AddError(result.FirstError.Description);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                // Stryker disable once Statement
                return;
            }

            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }

            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
            // Stryker disable once Statement
            return;
        }

        var response = new CreatedOilPatternResponse
        {
            OilPatternId = result.Value.Id.Value.ToString(),
            Name = result.Value.Name,
            Length = result.Value.Length,
            LengthCategory = result.Value.LengthCategory.Name,
            RatioCategory = result.Value.RatioCategory.Name
        };

        // 200, not 201 — no GetOilPattern-by-id endpoint exists to point a Location header at (same treatment as UploadSponsorLogoEndpoint)
        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}