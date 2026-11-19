using FastEndpoints;

using Neba.Api.Contracts.Uploads;

namespace Neba.Api.Features.Series.UploadSeriesLogo;

internal sealed class UploadSeriesLogoSummary : Summary<UploadSeriesLogoEndpoint>
{
    public UploadSeriesLogoSummary()
    {
        Summary = "Uploads a Series logo.";
        Description = "Stages an image file in blob storage ahead of Series creation or a logo change and returns a pointer to it. The pointer is orphaned (and later swept) unless it is included as Logo in a later CreateSeries, EditSeries, or EditSeriesLogo command. Requires the Series.CreateSeries, Series.EditSeries, or Series.EditSeriesLogo permission.";

        Response<UploadedFileResponse>(200, "File uploaded.");
        Response(400, "File missing, wrong content type, or too large.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user has none of the Series.CreateSeries, Series.EditSeries, or Series.EditSeriesLogo permissions.");
    }
}