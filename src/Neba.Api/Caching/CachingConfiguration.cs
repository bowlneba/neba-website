using Ardalis.SmartEnum.SystemTextJson;

using Community.Microsoft.Extensions.Caching.PostgreSql;

using Microsoft.Extensions.Caching.Hybrid;

using Neba.Api.Messaging;

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

        public void AddCaching(IConfiguration config)
        {
            var connectionString = config.GetConnectionString("bowlneba")
                ?? throw new InvalidOperationException("Cache connection string not found.");

            var cacheJsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                Converters = { new SmartEnumNameConverter<NameSuffix, string>() }
            };

            // Keyed registration: consumed by DefaultJsonSerializerFactory in Microsoft.Extensions.Caching.Hybrid.
            services.AddKeyedSingleton<System.Text.Json.JsonSerializerOptions>(
                HybridCacheSerializerOptionsKey.Key, cacheJsonOptions);

            services.AddHybridCache();

            services.AddDistributedPostgreSqlCache(options =>
            {
                options.ConnectionString = connectionString;
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