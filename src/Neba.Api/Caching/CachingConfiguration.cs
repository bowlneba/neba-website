using Ardalis.SmartEnum.SystemTextJson;

using Community.Microsoft.Extensions.Caching.PostgreSql;

using Microsoft.Extensions.Caching.Hybrid;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Messaging;

using Npgsql;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Caching;

internal static class HybridCacheSerializerOptionsKey
{
    // Key used by DefaultJsonSerializerFactory to resolve JsonSerializerOptions via keyed DI.
    // Must match typeof(IHybridCacheSerializer<>) as used internally by Microsoft.Extensions.Caching.Hybrid.
    internal static readonly Type Key = typeof(IHybridCacheSerializer<>);
}

internal static class CachingConfiguration
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection DecorateCachedQueryHandlers()
        {
            var descriptors = services
                .Where(d =>
                    d.ServiceType.IsGenericType &&
                    d.ServiceType.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
                .ToList();

            foreach (var serviceType in descriptors.Select(descriptor => descriptor.ServiceType))
            {
                var queryType = serviceType.GetGenericArguments()[0];
                var responseType = serviceType.GetGenericArguments()[1];

                var isCachedQuery = queryType.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICachedQuery<>));

                if (!isCachedQuery)
                {
                    continue;
                }

                var decoratorType = typeof(CachedQueryHandlerDecorator<,>).MakeGenericType(queryType, responseType);
                services.Decorate(serviceType, decoratorType);
            }

            return services;
        }

        public void AddCaching()
        {
            var cacheJsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                Converters = { new SmartEnumNameConverter<NameSuffix, string>() }
            };

            // Keyed registration: consumed by DefaultJsonSerializerFactory in Microsoft.Extensions.Caching.Hybrid.
            services.AddKeyedSingleton<System.Text.Json.JsonSerializerOptions>(
                HybridCacheSerializerOptionsKey.Key, cacheJsonOptions);

            services.AddHybridCache(options => options
                .MaximumPayloadBytes = 10 * 1024 * 1024);

            // Reuses the NpgsqlDataSource registered by AddDatabase() (via AddAzureNpgsqlDataSource)
            // instead of a raw ConnectionStrings:bowlneba value, so this connection gets the same
            // SSL enforcement and Azure AD token auth as EF Core - a bare connection string built
            // from configuration alone is missing both and gets rejected by Postgres's pg_hba rules.
            services.AddDistributedPostgreSqlCache((sp, options) =>
            {
                options.DataSourceFactory = sp.GetRequiredService<NpgsqlDataSource>;
                options.SchemaName = "cache";
                options.TableName = "distributed_cache";
                options.CreateInfrastructure = true;
            });

            services.AddFusionCache()
                .WithDefaultEntryOptions(options =>
                {
                    options.Duration = TimeSpan.FromHours(1);
                    options.FailSafeMaxDuration = TimeSpan.FromDays(1);
                    options.FailSafeThrottleDuration = TimeSpan.FromSeconds(30);
                })
                .WithSystemTextJsonSerializer(cacheJsonOptions)
                .WithRegisteredDistributedCache();
        }
    }
}