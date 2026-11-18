using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Series.EditSeries;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Security;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Series.EditSeries;

internal sealed class EditSeriesEndpoint(Messaging.ICommandHandler<EditSeriesCommand, Updated> commandHandler)
    : Endpoint<EditSeriesRequest>
{
    private readonly Messaging.ICommandHandler<EditSeriesCommand, Updated> _commandHandler = commandHandler;

    public override void Configure()
    {
        Put("{id}");
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditSeries.PolicyName);

        Description(description => description
            .WithName("EditSeries")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(EditSeriesRequest req, CancellationToken ct)
    {
        if (User.CallerUserId is not { } callerUserId)
        {
            await Send.UnauthorizedAsync(ct);

            // Stryker disable once Statement
            return;
        }

        var input = req.Series;

        var command = new EditSeriesCommand
        {
            SeriesId = new SeriesId(req.Id),
            CallerUserId = callerUserId,
            Name = input.Name,
            Logo = SeriesStageInputMapper.ToLogo(input.Logo),
            Stages = SeriesStageInputMapper.ToDefinitions(input.Stages),
            Finale = SeriesStageInputMapper.ToFinale(input.Finale),
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                await Send.NotFoundAsync(ct);
                // Stryker disable once Statement
                return;
            }

            if (result.FirstError.Type == ErrorType.Forbidden)
            {
                await Send.ForbiddenAsync(ct);
                // Stryker disable once Statement
                return;
            }

            await SeriesMutationResultSender.SendAsync(result.Errors, failure => AddError(failure), Send.ErrorsAsync, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}