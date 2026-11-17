using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Uploads;
using Neba.Api.Storage;
using Neba.Api.Uploads;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Series.UploadSeriesLogo;

internal sealed class UploadSeriesLogoEndpoint(IUploadStagingService stagingService, IFileStorageService fileStorageService)
    : Endpoint<UploadSeriesLogoRequest, UploadedFileResponse>
{
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
            .ProducesProblemDetails(StatusCodes.Status403Forbidden));
    }

    public override async Task HandleAsync(UploadSeriesLogoRequest req, CancellationToken ct)
    {
        var storedFile = await stagingService.StageUploadAsync(req.File, "bowlneba-public", "series/logo", null, ct);

        var response = new UploadedFileResponse
        {
            Container = storedFile.Container,
            Path = storedFile.Path,
            FileName = req.File.FileName,
            ContentType = storedFile.ContentType,
            SizeInBytes = storedFile.SizeInBytes,
            Url = fileStorageService.GetBlobUri(storedFile.Container, storedFile.Path)
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
