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
        var result = await LogoUploadProcessor.ResizeAndStageAsync(
            req.File, "series/logo", UploadSeriesLogoRequestValidator.AllowedContentTypes, imageResizer, stagingService, fileStorageService, ct);

        if (result.IsError)
        {
            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }

            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.OkAsync(result.Value, ct);
    }
}