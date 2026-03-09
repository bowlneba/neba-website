using Microsoft.Extensions.Options;

namespace Neba.Website.Server.Maps;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Unused private types or members should be static

internal static class MapsConfiguration
{
    extension(IServiceCollection services)
    {
        public void AddMaps(IConfiguration config)
        {
            services.AddOptions<AzureMapsSettings>()
                .Bind(config.GetSection(AzureMapsSettings.SectionName))
                .ValidateOnStart();

            services.AddSingleton(sp
                => sp.GetRequiredService<IOptions<AzureMapsSettings>>().Value);
        }
    }
}