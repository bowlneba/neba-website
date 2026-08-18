using Neba.Api.Legacy.Bowlers;
using Neba.Api.Legacy.HallOfFame;
using Neba.Api.Legacy.Tournaments;
using Neba.Api.Legacy.Tournaments.Complete;

namespace Neba.Api.Legacy;

internal static class LegacyEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapLegacyEndpoints()
        {
            app.MapNewBowler();
            app.MapUpdateBowler();

            app.MapNewTournament();
            app.MapSyncSquadScores();
            app.MapCompleteTournament();

            app.MapNewHallOfFameInduction();
        }
    }
}