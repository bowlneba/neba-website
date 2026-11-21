using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Features.Series.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Series.RemoveStageOverride;

internal sealed class RemoveStageOverrideEndpoint(Messaging.ICommandHandler<RemoveStageOverrideCommand, Deleted> commandHandler)
    : Endpoint<RemoveStageOverrideRequest>
{
    private readonly Messaging.ICommandHandler<RemoveStageOverrideCommand, Deleted> _commandHandler = commandHandler;

    public override void Configure()
    {
        Delete("{id}/stages/{stageId}/overrides/{overrideId}");
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.ManageAdvancement.PolicyName);

        Description(description => description
            .WithName("RemoveStageOverride")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict));
    }

    public override async Task HandleAsync(RemoveStageOverrideRequest req, CancellationToken ct)
    {
        var result = await _commandHandler.HandleAsync(
            new RemoveStageOverrideCommand
            {
                SeriesId = new SeriesId(req.Id),
                StageId = new SeriesStageId(req.StageId),
                OverrideId = new StageOverrideId(req.OverrideId),
                Version = req.Version!.Value,
            }, ct);

        if (result.IsError)
        {
            await SeriesAdvancementResultSender.SendAsync(result.Errors, failure => AddError(failure), Send.ErrorsAsync, Send.NotFoundAsync, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}