using Neba.Api.Legacy.Bowlers;
using Neba.Api.Legacy.Tournaments;

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
        }
    }
}