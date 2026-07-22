using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Tournaments.ListTournamentTypes;

namespace Neba.Api.Features.Tournaments.ListTournamentTypes;

internal sealed class ListTournamentTypesSummary
    : Summary<ListTournamentTypesEndpoint>
{
    public ListTournamentTypesSummary()
    {
        Summary = "Lists all active tournament types.";
        Description = "Retrieves the list of active tournament type formats, for use in tournament creation pickers.";

        Response(200, "The list of active tournament types.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<TournamentTypeSummaryResponse>
            {
                Items =
                [
                    new TournamentTypeSummaryResponse { Name = "Singles" }
                ]
            });
    }
}
