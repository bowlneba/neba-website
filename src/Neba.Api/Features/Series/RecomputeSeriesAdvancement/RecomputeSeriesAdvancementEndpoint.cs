using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Series.RecomputeSeriesAdvancement;
using Neba.Api.Features.Series.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Series.RecomputeSeriesAdvancement;

internal sealed class RecomputeSeriesAdvancementEndpoint(Messaging.ICommandHandler<RecomputeSeriesAdvancementCommand, Updated> commandHandler)
    : Endpoint<RecomputeSeriesAdvancementRequest>
{
    private readonly Messaging.ICommandHandler<RecomputeSeriesAdvancementCommand, Updated> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("{id}/stages/{stageId}/recomputations");
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditSeries.PolicyName);

        Description(description => description
            .WithName("RecomputeSeriesAdvancement")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict));
    }

    public override async Task HandleAsync(RecomputeSeriesAdvancementRequest req, CancellationToken ct)
    {
        var result = await _commandHandler.HandleAsync(
            new RecomputeSeriesAdvancementCommand
            {
                SeriesId = new SeriesId(req.Id),
                StageId = new SeriesStageId(req.StageId),
                Version = req.Recomputation.Version,
                ConfirmResetOfficial = req.Recomputation.ConfirmResetOfficial,
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