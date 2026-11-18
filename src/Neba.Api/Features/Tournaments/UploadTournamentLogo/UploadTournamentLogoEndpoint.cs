using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Tournaments.UploadTournamentLogo;
using Neba.Api.Contracts.Uploads;
using Neba.Api.Storage;
using Neba.Api.Uploads;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.UploadTournamentLogo;

internal sealed class UploadTournamentLogoEndpoint(IUploadStagingService stagingService, IFileStorageService fileStorageService, IImageResizer imageResizer)
    : LogoUploadEndpoint<UploadTournamentLogoRequest>
{
    public override void Configure()
    {
        Post("logo");
        Group<TournamentsEndpointGroup>();
        AllowFileUploads();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CanUploadTournamentLogoPolicyName);

        Description(description => description
            .WithName("UploadTournamentLogo")
            .WithTags("Admin")
            .Produces<UploadedFileResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(UploadTournamentLogoRequest req, CancellationToken ct)
    {
        var result = await LogoUploadProcessor.ResizeAndStageAsync(
            req.File, "tournaments/logo", UploadTournamentLogoRequestValidator.AllowedContentTypes, imageResizer, stagingService, fileStorageService, ct);

        await SendResultAsync(result, ct);
    }
}