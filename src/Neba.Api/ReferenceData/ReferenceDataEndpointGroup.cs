using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.ReferenceData;

internal sealed class ReferenceDataEndpointGroup
    : SubGroup<BaseEndpointGroup>
{
    public ReferenceDataEndpointGroup()
    {
        VersionSets.CreateApi("ReferenceData", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("reference-data", endpoint => endpoint
            .Description(description => description
                .WithTags("ReferenceData")
                .ProducesProblemDetails(500)));
    }
}