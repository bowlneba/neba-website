using FastEndpoints;

using Neba.Api.Contracts.Uploads;

namespace Neba.Api.Features.Sponsors.UploadSponsorLogo;

internal sealed class UploadSponsorLogoSummary : Summary<UploadSponsorLogoEndpoint>
{
    public UploadSponsorLogoSummary()
    {
        Summary = "Uploads a sponsor logo.";
        Description = "Stages an image file in blob storage ahead of sponsor creation and returns a pointer to it. The pointer is orphaned (and later swept) unless it's included as Logo in a subsequent CreateSponsor command. Requires the Sponsors.CreateSponsor permission.";

        Response<UploadedFileResponse>(200, "File uploaded.");
        Response(400, "File missing, wrong content type, or too large.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Sponsors.CreateSponsor permission.");
    }
}