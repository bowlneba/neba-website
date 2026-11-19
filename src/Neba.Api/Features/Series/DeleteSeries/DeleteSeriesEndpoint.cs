using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Features.Series.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Series.DeleteSeries;

internal sealed class DeleteSeriesEndpoint(Messaging.ICommandHandler<DeleteSeriesCommand, Deleted> commandHandler)
    : Endpoint<DeleteSeriesRequest>
{
    private readonly Messaging.ICommandHandler<DeleteSeriesCommand, Deleted> _commandHandler = commandHandler;

    public override void Configure()
    {
        Delete("{id}");
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.DeleteSeries.PolicyName);

        Description(description => description
            .WithName("DeleteSeries")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status409Conflict));
    }

    public override async Task HandleAsync(DeleteSeriesRequest req, CancellationToken ct)
    {
        var result = await _commandHandler.HandleAsync(new DeleteSeriesCommand { SeriesId = new SeriesId(req.Id) }, ct);

        if (result.IsError)
        {
            await SeriesMutationResultSender.SendAsync(result.Errors, failure => AddError(failure), Send.ErrorsAsync, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}