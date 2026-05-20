using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Features.Seasons;

internal sealed class SeasonsEndpointGroup
    : SubGroup<BaseEndpointGroup>
{
    public SeasonsEndpointGroup()
    {
        VersionSets.CreateApi("Seasons", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("seasons", endpoint => endpoint
            .Description(description => description
                .WithTags("Seasons")
                .ProducesProblemDetails(500)));
    }
}