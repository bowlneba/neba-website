using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.HallOfFame.EditHallOfFamePhoto;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.Storage.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.HallOfFame.EditHallOfFamePhoto;

internal sealed class EditHallOfFamePhotoEndpoint(Messaging.ICommandHandler<EditHallOfFamePhotoCommand, Updated> commandHandler)
    : Endpoint<EditHallOfFamePhotoRequest>
{
    private readonly Messaging.ICommandHandler<EditHallOfFamePhotoCommand, Updated> _commandHandler = commandHandler;

    public override void Configure()
    {
        Put("{id}/photo");
        Group<HallOfFameEndpointGroup>();

        Options(options => options
            .WithVersionSet("HallOfFame")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditHallOfFamePhoto.PolicyName);

        Description(description => description
            .WithName("EditHallOfFamePhoto")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(EditHallOfFamePhotoRequest req, CancellationToken ct)
    {
        var photo = req.Input.Photo;

        var command = new EditHallOfFamePhotoCommand
        {
            HallOfFameId = new HallOfFameId(req.Id),
            Photo = photo is null
                ? null
                : new StoredFile
                {
                    Container = photo.Container,
                    Path = photo.Path,
                    ContentType = photo.ContentType,
                    SizeInBytes = photo.SizeInBytes
                },
            FocalX = req.Input.FocalX,
            FocalY = req.Input.FocalY,
            Zoom = req.Input.Zoom
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

            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }

            await Send.ErrorsAsync(
                result.FirstError.Type == ErrorType.Conflict
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status422UnprocessableEntity,
                ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}
