using Microsoft.Extensions.DependencyInjection;

using Neba.Application.Messaging;
using Neba.Application.Stats;
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

            // This is a place holder until we have tournaments in the database.  It provides the number of tournaments for a given season, which is used in the stats calculations.
            services.AddSingleton<ITournamentQueries, TournamentCount>();

            return services;
        }

        internal void AddServices()
        {
            services.AddScoped<ISeasonStatsService, SeasonStatsService>();
        }
    }
}