using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Series.MakeStageOfficial;
using Neba.Api.Features.Series.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Series.MakeStageOfficial;

internal sealed class MakeStageOfficialEndpoint(Messaging.ICommandHandler<MakeStageOfficialCommand, Updated> commandHandler)
    : Endpoint<MakeStageOfficialRequest>
{
    private readonly Messaging.ICommandHandler<MakeStageOfficialCommand, Updated> _commandHandler = commandHandler;

    public override void Configure()
    {
        Put("{id}/stages/{stageId}/official");
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.MakeStageOfficial.PolicyName);

        Description(description => description
            .WithName("MakeStageOfficial")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict));
    }

    public override async Task HandleAsync(MakeStageOfficialRequest req, CancellationToken ct)
    {
        var result = await _commandHandler.HandleAsync(
            new MakeStageOfficialCommand
            {
                SeriesId = new SeriesId(req.Id),
                StageId = new SeriesStageId(req.StageId),
                Version = req.Official.Version,
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