using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Features.Bowlers;

internal sealed class BowlersEndpointGroup
    : SubGroup<BaseEndpointGroup>
{
    public BowlersEndpointGroup()
    {
        VersionSets.CreateApi("Bowlers", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("bowlers", endpoint => endpoint
            .Description(description => description
                .WithTags("Bowlers")
                .ProducesProblemDetails(500)));
    }
}
