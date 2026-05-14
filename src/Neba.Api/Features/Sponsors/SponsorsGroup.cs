using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Features.Sponsors;

internal sealed class SponsorsGroup
    : SubGroup<BaseGroup>
{
    public SponsorsGroup()
    {
        VersionSets.CreateApi("Sponsors", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("sponsors", endpoint => endpoint
            .Description(description => description
                .WithTags("Sponsors")
                .ProducesProblemDetails(500)));
    }
}