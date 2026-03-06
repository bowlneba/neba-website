using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.BowlingCenters;

internal sealed class BowlingCentersGroup
    : SubGroup<BaseGroup>
{
    public BowlingCentersGroup()
    {
        VersionSets.CreateApi("BowlingCenters", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("bowling-centers", endpoint => endpoint
            .Description(description => description
                .WithTags("BowlingCenters")
                .ProducesProblemDetails(500)));
    }
}
