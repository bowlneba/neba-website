using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Messaging;
using Neba.Api.Stats;
using Neba.Api.Stats.BoyProgression;
using Neba.Api.Tournaments;

namespace Neba.Api;

/// <summary>
/// Extension methods to add application dependencies to the service collection.
/// </summary>
public static class ApplicationConfiguration
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds application dependencies to the service collection.
        /// </summary>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddApplication()
        {
            services.AddMessaging();
            services.AddServices();

            return services;
        }

        internal void AddServices()
        {
            services.AddScoped<ISeasonStatsService, SeasonStatsService>();
            services.AddScoped<IBowlerOfTheYearProgressionService, BowlerOfTheYearProgressionService>();
        }
    }
}