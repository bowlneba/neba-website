using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.News.UploadArticleHeaderImage;
using Neba.Api.Contracts.Uploads;
using Neba.Api.Uploads;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.News.UploadArticleHeaderImage;

internal sealed class UploadArticleHeaderImageEndpoint(IUploadStagingService stagingService)
    : Endpoint<UploadArticleHeaderImageRequest, UploadedFileResponse>
{
    public override void Configure()
    {
        Post("header-image");
        Group<NewsEndpointGroup>();
        AllowFileUploads();

        Options(options => options
            .WithVersionSet("News")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CreateArticle.PolicyName);

        Description(description => description
            .WithName("UploadArticleHeaderImage")
            .WithTags("Admin")
            .Produces<UploadedFileResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden));
    }

    public override async Task HandleAsync(UploadArticleHeaderImageRequest req, CancellationToken ct)
    {
        var storedFile = await stagingService.StageUploadAsync(req.File, "news", "header", null, ct);

        var response = new UploadedFileResponse
        {
            Container = storedFile.Container,
            Path = storedFile.Path,
            ContentType = storedFile.ContentType,
            SizeInBytes = storedFile.SizeInBytes
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}