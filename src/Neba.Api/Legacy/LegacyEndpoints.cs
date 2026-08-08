using Neba.Api.Legacy.Bowlers;

namespace Neba.Api.Legacy;

internal static class LegacyEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapLegacyEndpoints()
        {
            app.MapNewBowler();
        }
    }
}