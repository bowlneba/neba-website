using Neba.Api.Legacy.Bowlers;

namespace Neba.Api.Legacy;

internal static class LegacyEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
#pragma warning disable CA1822
        public void MapLegacyEndpoints()
        {
            app.MapNewBowler();
        }
#pragma warning restore CA1822
    }
}