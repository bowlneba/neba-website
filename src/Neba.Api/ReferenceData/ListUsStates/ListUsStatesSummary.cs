using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.ReferenceData;

namespace Neba.Api.ReferenceData.ListUsStates;

internal sealed class ListUsStatesSummary
    : Summary<ListUsStatesEndpoint>
{
    public ListUsStatesSummary()
    {
        Summary = "Lists all US states.";
        Description = "Retrieves the full list of US states, including the District of Columbia, for populating state dropdowns.";

        Response(200, "The list of US states.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<UsStateResponse>
            {
                Items =
                [
                    new UsStateResponse { Name = "Massachusetts", Code = "MA" }
                ],
            });
    }
}
