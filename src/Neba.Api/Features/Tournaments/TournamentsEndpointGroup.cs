using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Features.Tournaments;

internal sealed class TournamentsEndpointGroup 
    : SubGroup<BaseEndpointGroup>
{
    public TournamentsEndpointGroup()
    {
        VersionSets.CreateApi("Tournaments", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("tournaments", endpoint => endpoint
            .Description(description => description
                .WithTags("Tournaments")
                .ProducesProblemDetails(500)));
    }
}