using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Features.Cache;

internal sealed class CacheEndpointGroup
    : SubGroup<BaseEndpointGroup>
{
    public CacheEndpointGroup()
    {
        VersionSets.CreateApi("Cache", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("cache", endpoint => endpoint
            .Description(description => description
                .WithTags("Cache")
                .ProducesProblemDetails(500)));
    }
}