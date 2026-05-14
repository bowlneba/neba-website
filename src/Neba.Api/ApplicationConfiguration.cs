using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.Api.Messaging;

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
            services.AddSingleton<ISeasonStatsCalculator, SeasonStatsCalculator>();
            services.AddSingleton<IBowlerOfTheYearRaceCalculator, BowlerOfTheYearRaceCalculator>();
        }
    }
}