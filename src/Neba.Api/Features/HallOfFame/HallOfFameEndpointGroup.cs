using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Features.HallOfFame;

internal sealed class HallOfFameEndpointGroup
    : SubGroup<BaseEndpointGroup>
{
    public HallOfFameEndpointGroup()
    {
        VersionSets.CreateApi("HallOfFame", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("hall-of-fame", endpoint => endpoint
            .Description(description => description
                .WithTags("HallOfFame")
                .ProducesProblemDetails(500)));
    }
}