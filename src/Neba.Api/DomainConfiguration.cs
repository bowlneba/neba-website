using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api;

/// <summary>
/// Extension methods to add application dependencies to the service collection.
/// </summary>
public static class DomainConfiguration
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds application dependencies to the service collection.
        /// </summary>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddDomain()
        {
            services.AddScoped<ITournamentValidationService, TournamentValidationService>();

            return services;
        }
    }
}