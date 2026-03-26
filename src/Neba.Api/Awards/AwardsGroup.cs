using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Awards;

internal sealed class AwardsGroup
    : SubGroup<BaseGroup>
{
    public AwardsGroup()
    {
        VersionSets.CreateApi("Awards", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("awards", endpoint => endpoint
            .Description(description => description
                .WithTags("Awards")
                .ProducesProblemDetails(500)));
    }
}