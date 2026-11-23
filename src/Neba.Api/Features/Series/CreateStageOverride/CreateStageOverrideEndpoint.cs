using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Series.CreateStageOverride;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Security;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Series.CreateStageOverride;

internal sealed class CreateStageOverrideEndpoint(Messaging.ICommandHandler<CreateStageOverrideCommand, CreatedStageOverride> commandHandler)
    : Endpoint<CreateStageOverrideRequest, CreatedStageOverrideResponse>
{
    private readonly Messaging.ICommandHandler<CreateStageOverrideCommand, CreatedStageOverride> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("{id}/stages/{stageId}/overrides");
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.ManageAdvancement.PolicyName);

        Description(description => description
            .WithName("CreateStageOverride")
            .WithTags("Admin")
            .Produces<CreatedStageOverrideResponse>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CreateStageOverrideRequest req, CancellationToken ct)
    {
        if (User.CallerUserId is not { } callerUserId)
        {
            await Send.UnauthorizedAsync(ct);
            // Stryker disable once Statement
            return;
        }

        var input = req.StageOverride;

        var result = await _commandHandler.HandleAsync(
            new CreateStageOverrideCommand
            {
                SeriesId = new SeriesId(req.Id),
                StageId = new SeriesStageId(req.StageId),
                Kind = OverrideKind.FromValue(input.Kind),
                BowlerId = new BowlerId(input.BowlerId),
                Reason = input.Reason,
                CallerUserId = callerUserId,
                Version = input.Version,
            }, ct);

        if (result.IsError)
        {
            await SeriesAdvancementResultSender.SendAsync(result.Errors, failure => AddError(failure), Send.ErrorsAsync, Send.NotFoundAsync, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.CreatedAtAsync(
            "GetSeriesAdvancement",
            routeValues: new { id = req.Id },
            responseBody: new CreatedStageOverrideResponse
            {
                OverrideId = result.Value.OverrideId.ToString(),
                Version = result.Value.Version,
            },
            cancellation: ct);
    }
}