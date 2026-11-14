using FastEndpoints;

namespace Neba.Api.Features.Tournaments.ListUpcomingTournaments;

internal sealed class ListUpcomingTournamentsSummary
    : Summary<ListUpcomingTournamentsEndpoint>
{
    public ListUpcomingTournamentsSummary()
    {
        Summary = "Lists upcoming tournaments across seasons.";
        Description = "Retrieves every tournament that has not ended yet, in the current season and any later season, ordered by start date. Each item has the same shape as the season tournament list.";

        Response(200, "The list of upcoming tournaments.");
    }
}