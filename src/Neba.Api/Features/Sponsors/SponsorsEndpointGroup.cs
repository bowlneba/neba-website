using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Features.Sponsors;

internal sealed class SponsorsEndpointGroup
    : SubGroup<BaseEndpointGroup>
{
    public SponsorsEndpointGroup()
    {
        VersionSets.CreateApi("Sponsors", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("sponsors", endpoint => endpoint
            .Description(description => description
                .WithTags("Sponsors")
                .ProducesProblemDetails(500)));
    }
}