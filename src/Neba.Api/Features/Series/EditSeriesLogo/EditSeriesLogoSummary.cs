using FastEndpoints;

using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Contracts.Series.EditSeriesLogo;

namespace Neba.Api.Features.Series.EditSeriesLogo;

internal sealed class EditSeriesLogoSummary : Summary<EditSeriesLogoEndpoint>
{
    public EditSeriesLogoSummary()
    {
        Summary = "Replaces or removes a Series' logo.";
        Description = "Sets the Series' logo to a previously uploaded file, or clears it when the logo is null. Does not change any other Series field, and works while the Series is locked. Requires the Series.EditSeriesLogo permission.";

        ExampleRequest = new EditSeriesLogoRequest
        {
            Id = "01JZ8K3M4N5P6Q7R8S9T0V1W2X",
            Input = new EditSeriesLogoInput
            {
                Logo = new SeriesLogoInput
                {
                    Container = "bowlneba-public",
                    Path = "series/logo/01JZ8K3M4N5P6Q7R8S9T0V1W2X.png",
                    ContentType = "image/png",
                    SizeInBytes = 48213
                }
            }
        };

        Response(204, "Series logo updated.");
        Response(400, "Id or the logo fields failed structural validation.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Series.EditSeriesLogo permission.");
        Response(404, "No Series exists with the given Id.");
        Response(422, "The logo is not a file uploaded through the logo upload endpoint.");
    }
}
