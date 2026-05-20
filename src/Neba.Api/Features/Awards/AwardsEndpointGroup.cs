using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Features.Awards;

internal sealed class AwardsEndpointGroup
    : SubGroup<BaseEndpointGroup>
{
    public AwardsEndpointGroup()
    {
        VersionSets.CreateApi("Awards", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("awards", endpoint => endpoint
            .Description(description => description
                .WithTags("Awards")
                .ProducesProblemDetails(500)));
    }
}