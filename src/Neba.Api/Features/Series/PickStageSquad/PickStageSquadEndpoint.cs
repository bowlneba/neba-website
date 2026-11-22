using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Series.PickStageSquad;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Series.PickStageSquad;

internal sealed class PickStageSquadEndpoint(Messaging.ICommandHandler<PickStageSquadCommand, Updated> commandHandler)
    : Endpoint<PickStageSquadRequest>
{
    private readonly Messaging.ICommandHandler<PickStageSquadCommand, Updated> _commandHandler = commandHandler;

    public override void Configure()
    {
        Put("{id}/stages/{stageId}/squad");
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditSeries.PolicyName);

        Description(description => description
            .WithName("PickStageSquad")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(PickStageSquadRequest req, CancellationToken ct)
    {
        var result = await _commandHandler.HandleAsync(
            new PickStageSquadCommand
            {
                SeriesId = new SeriesId(req.Id),
                StageId = new SeriesStageId(req.StageId),
                SquadId = new SquadId(req.Squad.SquadId),
                Version = req.Squad.Version,
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