using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Series.EditSeriesLogo;
using Neba.Api.Features.Series.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Series.EditSeriesLogo;

internal sealed class EditSeriesLogoEndpoint(Messaging.ICommandHandler<EditSeriesLogoCommand, Updated> commandHandler)
    : Endpoint<EditSeriesLogoRequest>
{
    private readonly Messaging.ICommandHandler<EditSeriesLogoCommand, Updated> _commandHandler = commandHandler;

    public override void Configure()
    {
        Put("{id}/logo");
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditSeriesLogo.PolicyName);

        Description(description => description
            .WithName("EditSeriesLogo")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(EditSeriesLogoRequest req, CancellationToken ct)
    {
        var command = new EditSeriesLogoCommand
        {
            SeriesId = new SeriesId(req.Id),
            Logo = SeriesStageInputMapper.ToLogo(req.Input.Logo),
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

            await SeriesMutationResultSender.SendAsync(result.Errors, failure => AddError(failure), Send.ErrorsAsync, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}