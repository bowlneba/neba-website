using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Documents;

internal sealed class DocumentsGroup
    : SubGroup<BaseGroup>
{
    public DocumentsGroup()
    {
        VersionSets.CreateApi("Documents", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("documents", endpoint => endpoint
            .Description(description => description
                .WithTags("Documents")
                .ProducesProblemDetails(500)));
    }
}