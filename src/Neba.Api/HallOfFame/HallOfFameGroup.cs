using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.HallOfFame;

internal sealed class HallOfFameGroup
    : SubGroup<BaseGroup>
{
    public HallOfFameGroup()
    {
        VersionSets.CreateApi("HallOfFame", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("hall-of-fame", endpoint => endpoint
            .Description(description => description
                .WithTags("HallOfFame")
                .ProducesProblemDetails(500)));
    }
}
