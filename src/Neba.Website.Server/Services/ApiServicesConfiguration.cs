using System.Text.Json;

using Microsoft.Extensions.Options;

using Neba.Api.Contracts.Weather;

using Refit;

namespace Neba.Website.Server.Services;

#pragma warning disable S1144 // Interfaces should not have public constructors
#pragma warning disable S2325 // Private types or members should not be static

internal static class ApiServicesConfiguration
{
    private static readonly RefitSettings RefitSettings = new()
    {
        ContentSerializer = new SystemTextJsonContentSerializer(
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        )
    };

    extension(IServiceCollection services)
    {
        public IServiceCollection AddApiServices(IConfiguration configuration)
        {
            services.AddOptions<NebaApiConfiguration>()
                .Bind(configuration.GetSection("NebaApi"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<NebaApiConfiguration>>().Value);
            services.AddScoped<ApiExecutor>();

            services.RegisterApiEndpoint<IWeatherApi>();

            return services;
        }

        private IServiceCollection RegisterApiEndpoint<TApi>()
            where TApi : class
        {
            services
                .AddRefitClient<TApi>(RefitSettings)
                .ConfigureHttpClient((sp, client) =>
                {
                    var apiConfig = sp.GetRequiredService<NebaApiConfiguration>();
                    client.BaseAddress = new Uri(apiConfig.BaseUrl);
                });

            return services;
        }
    }
}