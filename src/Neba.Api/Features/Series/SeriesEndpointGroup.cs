using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Features.Series;

internal sealed class SeriesEndpointGroup
    : SubGroup<BaseEndpointGroup>
{
    public SeriesEndpointGroup()
    {
        VersionSets.CreateApi("Series", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("series", endpoint => endpoint
            .Description(description => description
                .WithTags("Series")
                .ProducesProblemDetails(500)));
    }
}
