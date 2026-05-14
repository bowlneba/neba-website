using Microsoft.Extensions.DependencyInjection;

using Neba.Application.Messaging;
using Neba.Application.Stats;
using Neba.Application.Stats.BoyProgression;
using Neba.Application.Tournaments;

namespace Neba.Application;

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