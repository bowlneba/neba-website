using System.Text.Json;

using Microsoft.Extensions.Options;

using Neba.Api.Contracts.Awards;
using Neba.Api.Contracts.BowlingCenters;
using Neba.Api.Contracts.Documents;
using Neba.Api.Contracts.HallOfFame;
using Neba.Api.Contracts.Seasons;
using Neba.Api.Contracts.Sponsors;
using Neba.Api.Contracts.Stats;
using Neba.Api.Contracts.Tournaments;

using Refit;

namespace Neba.Website.Server.Services;

internal static class ApiServicesConfiguration
{
    private static readonly RefitSettings RefitSettings = new()
    {
        ContentSerializer = new SystemTextJsonContentSerializer(
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new Cysharp.Serialization.Json.UlidJsonConverter() }
            }
        )
    };

    extension(IServiceCollection services)
    {
        public IServiceCollection AddApiServices(IConfiguration configuration)
        {
            services.AddOptions<NebaApiConfiguration>()
                .Bind(configuration.GetSection("NebaApi"))
                .Validate(config => config.BaseUrl?.IsAbsoluteUri ?? false, "BaseUrl must be a valid absolute URI")
                .ValidateOnStart();

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<NebaApiConfiguration>>().Value);
            services.AddScoped<ApiExecutor>();

            services.RegisterApiEndpoint<IDocumentsApi>();
            services.RegisterApiEndpoint<IBowlingCentersApi>();
            services.RegisterApiEndpoint<IHallOfFameApi>();
            services.RegisterApiEndpoint<IAwardsApi>();
            services.RegisterApiEndpoint<ISeasonsApi>();
            services.RegisterApiEndpoint<ISponsorsApi>();
            services.RegisterApiEndpoint<IStatsApi>();
            services.RegisterApiEndpoint<ITournamentsApi>();

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
                    client.BaseAddress = apiConfig.BaseUrl;
                });

            return services;
        }
    }
}