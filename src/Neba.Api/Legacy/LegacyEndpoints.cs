namespace Neba.Api.Legacy;

internal static class LegacyEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        // CA1822 fires because this doesn't touch `app` yet — it will, once the first
        // Legacy/*.cs action adds its own `app.MapXxx()` line below.
        #pragma warning disable CA1822
        public void MapLegacyEndpoints()
        {
            // One line per Legacy/*.cs action file
            // added by each action's own follow-up branch
        }
        #pragma warning restore CA1822
    }
}
