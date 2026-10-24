using FastEndpoints;

using Neba.Api.Contracts.Uploads;

namespace Neba.Api.Features.HallOfFame.UploadHallOfFamePhoto;

internal sealed class UploadHallOfFamePhotoSummary : Summary<UploadHallOfFamePhotoEndpoint>
{
    public UploadHallOfFamePhotoSummary()
    {
        Summary = "Uploads a Hall of Fame photo.";
        Description = "Stages an image file in blob storage ahead of a photo change and returns a pointer to it. The pointer is orphaned (and later swept) unless it's included as Photo in a subsequent EditHallOfFamePhoto command. Requires the HallOfFame.EditHallOfFamePhoto permission.";

        Response<UploadedFileResponse>(200, "File uploaded.");
        Response(400, "File missing, wrong content type, or too large.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the HallOfFame.EditHallOfFamePhoto permission.");
    }
}
