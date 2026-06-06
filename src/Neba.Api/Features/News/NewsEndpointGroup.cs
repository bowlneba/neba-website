using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Features.News;

internal sealed class NewsEndpointGroup
    : SubGroup<BaseEndpointGroup>
{
    public NewsEndpointGroup()
    {
        VersionSets.CreateApi("News", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("news", endpoint => endpoint
            .Description(description => description
                .WithTags("News")
                .ProducesProblemDetails(500)));
    }
}