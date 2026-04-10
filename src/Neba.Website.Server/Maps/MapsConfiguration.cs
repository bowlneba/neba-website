using Microsoft.Extensions.Options;

namespace Neba.Website.Server.Maps;

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