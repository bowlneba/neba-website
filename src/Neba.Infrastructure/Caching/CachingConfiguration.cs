using Community.Microsoft.Extensions.Caching.PostgreSql;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Infrastructure.Caching;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Methods and properties that don't access instance data should be static

internal static class CachingConfiguration
{
    extension(IServiceCollection services)
    {
        public void AddCaching(IConfiguration config)
        {
            services.AddDistributedPostgreSqlCache(options =>
            {
                options.ConnectionString = config.GetConnectionString("bowlneba-cache")
                    ?? throw new InvalidOperationException("Cache connection string not found.");

                options.SchemaName = "public";
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
                .WithSystemTextJsonSerializer()
                .WithRegisteredDistributedCache();
        }
    }
}