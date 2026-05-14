using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Features.Stats;

internal sealed class StatsGroup : SubGroup<BaseGroup>
{
    public StatsGroup()
    {
        VersionSets.CreateApi("Stats", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("stats", endpoint => endpoint
            .Description(description => description
                .WithTags("Stats")
                .ProducesProblemDetails(500)));
    }
}