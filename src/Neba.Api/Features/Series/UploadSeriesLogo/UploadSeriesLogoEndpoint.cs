using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Uploads;
using Neba.Api.Storage;
using Neba.Api.Uploads;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Series.UploadSeriesLogo;

internal sealed class UploadSeriesLogoEndpoint(IUploadStagingService stagingService, IFileStorageService fileStorageService, IImageResizer imageResizer)
    : Endpoint<UploadSeriesLogoRequest, UploadedFileResponse>
{
    /// <summary>
    /// The longest side a stored logo keeps. The largest tile is 96px, so 3x covers a high-density screen.
    /// </summary>
    internal const int MaxLogoDimension = 512;

    public override void Configure()
    {
        Post("logo");
        Group<SeriesEndpointGroup>();
        AllowFileUploads();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CanUploadSeriesLogoPolicyName);

        Description(description => description
            .WithName("UploadSeriesLogo")
            .WithTags("Admin")
            .Produces<UploadedFileResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(UploadSeriesLogoRequest req, CancellationToken ct)
    {
        await using var upload = req.File.OpenReadStream();
        var resized = await imageResizer.ResizeLogoAsync(upload, MaxLogoDimension, UploadSeriesLogoRequestValidator.AllowedContentTypes, ct);

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
            content, fileName, resized.Value.ContentType, "bowlneba-public", "series/logo", null, ct);

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