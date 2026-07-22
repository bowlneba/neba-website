using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Tournaments.UploadTournamentLogo;
using Neba.Api.Contracts.Uploads;
using Neba.Api.Storage;
using Neba.Api.Uploads;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.UploadTournamentLogo;

internal sealed class UploadTournamentLogoEndpoint(IUploadStagingService stagingService, IFileStorageService fileStorageService)
    : Endpoint<UploadTournamentLogoRequest, UploadedFileResponse>
{
    public override void Configure()
    {
        Post("logo");
        Group<TournamentsEndpointGroup>();
        AllowFileUploads();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CreateTournament.PolicyName);

        Description(description => description
            .WithName("UploadTournamentLogo")
            .WithTags("Admin")
            .Produces<UploadedFileResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden));
    }

    public override async Task HandleAsync(UploadTournamentLogoRequest req, CancellationToken ct)
    {
        var storedFile = await stagingService.StageUploadAsync(req.File, "bowlneba-public", "tournaments/logo", null, ct);

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
