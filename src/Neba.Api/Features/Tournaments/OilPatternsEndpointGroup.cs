using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Features.Tournaments;

internal sealed class OilPatternsEndpointGroup
    : SubGroup<BaseEndpointGroup>
{
    public OilPatternsEndpointGroup()
    {
        VersionSets.CreateApi("OilPatterns", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("oil-patterns", endpoint => endpoint
            .Description(description => description
                .WithTags("OilPatterns")
                .ProducesProblemDetails(500)));
    }
}