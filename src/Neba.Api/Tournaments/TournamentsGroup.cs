using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Tournaments;

internal sealed class TournamentsGroup : SubGroup<BaseGroup>
{
    public TournamentsGroup()
    {
        VersionSets.CreateApi("Tournaments", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("tournaments", endpoint => endpoint
            .Description(description => description
                .WithTags("Tournaments")
                .ProducesProblemDetails(500)));
    }
}