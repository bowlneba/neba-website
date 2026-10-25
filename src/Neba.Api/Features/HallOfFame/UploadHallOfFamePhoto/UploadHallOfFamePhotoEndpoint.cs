using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.HallOfFame.UploadHallOfFamePhoto;
using Neba.Api.Contracts.Uploads;
using Neba.Api.Storage;
using Neba.Api.Uploads;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.HallOfFame.UploadHallOfFamePhoto;

internal sealed class UploadHallOfFamePhotoEndpoint(IUploadStagingService stagingService, IFileStorageService fileStorageService, IImageResizer imageResizer)
    : Endpoint<UploadHallOfFamePhotoRequest, UploadedFileResponse>
{
    /// <summary>
    /// The longest side a stored photo keeps. A round avatar shows a window of the photo zoomed up to 3x on a
    /// high-density screen, and the focal point picker shows it larger still.
    /// </summary>
    internal const int MaxPhotoDimension = 1600;

    public override void Configure()
    {
        Post("photos");
        Group<HallOfFameEndpointGroup>();
        AllowFileUploads();

        Options(options => options
            .WithVersionSet("HallOfFame")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditHallOfFamePhoto.PolicyName);

        Description(description => description
            .WithName("UploadHallOfFamePhoto")
            .WithTags("Admin")
            .Produces<UploadedFileResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(UploadHallOfFamePhotoRequest req, CancellationToken ct)
    {
        await using var upload = req.File.OpenReadStream();
        var resized = await imageResizer.ResizeAsync(upload, MaxPhotoDimension, UploadHallOfFamePhotoRequestValidator.AllowedContentTypes, ct);

        if (resized.IsError)
        {
            foreach (var error in resized.Errors)
            {
                AddError(error.Description);
            }

            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
            // Stryker disable once Statement
            return;
        }

        // The resizer re-encodes in the format the bytes decoded as, which can differ from the client's file name.
        var fileName = $"{Path.GetFileNameWithoutExtension(req.File.FileName)}.{resized.Value.FileExtension}";

        await using var content = new MemoryStream(resized.Value.Content);
        var storedFile = await stagingService.StageUploadAsync(
            content, fileName, resized.Value.ContentType, "bowlneba-public", "hall-of-fame/photo", null, ct);

        var response = new UploadedFileResponse
        {
            Container = storedFile.Container,
            Path = storedFile.Path,
            FileName = fileName,
            ContentType = storedFile.ContentType,
            SizeInBytes = storedFile.SizeInBytes,
            Url = fileStorageService.GetBlobUri(storedFile.Container, storedFile.Path)
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}