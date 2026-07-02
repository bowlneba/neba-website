using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Security;

internal sealed class SecurityEndpointGroup
    : SubGroup<BaseEndpointGroup>
{
    public SecurityEndpointGroup()
    {
        VersionSets.CreateApi("Security", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("security", endpoint => endpoint
            .Description(description => description
                .WithTags("security")
                .ProducesProblemDetails(500)));
    }
}