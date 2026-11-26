using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Tournaments.ListEligibleBowlers;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

internal sealed class ListEligibleBowlersSummary
    : Summary<ListEligibleBowlersEndpoint>
{
    public ListEligibleBowlersSummary()
    {
        Summary = "Lists the bowlers eligible for an Invitational.";
        Description = $"Returns the bowlers with a result in {Tournament.InvitationalEligibilityThreshold} or more tournaments of the Invitational's season, sorted by last name then first name. "
            + "Only a scheduled Invitational has a list; any other tournament returns null requirements and no bowlers. "
            + "Only the display name and USBC ID return. Tournament counts, ineligible bowlers and dates of birth never do.";

        ExampleRequest = new ListEligibleBowlersRequest { TournamentId = "01JZ8K3M4N5P6Q7R8S9T0V1W2X" };

        Response(200, "The requirements and eligible bowlers. Requirements are null when the tournament has no list.",
            contentType: MediaTypeNames.Application.Json,
            example: new EligibleBowlersResponse
            {
                Requirements = new EligibilityRequirementsResponse { MinTournamentsCount = Tournament.InvitationalEligibilityThreshold },
                Bowlers =
                [
                    new EligibleBowlerSummaryResponse { Name = "Jane Smith", UsbcId = "4821-09317" },
                    new EligibleBowlerSummaryResponse { Name = "Sam Jones", UsbcId = null }
                ]
            });

        Response(400, "The tournament ID is not a 26-character ULID.");
        Response(404, "The tournament was not found.");
    }
}