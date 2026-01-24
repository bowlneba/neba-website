using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Neba.Infrastructure.Database;

using Npgsql;

namespace Neba.Infrastructure;

#pragma warning disable S1144 // Unused private types or members should be removed

/// <summary>
/// Extension methods to add infrastructure dependencies to the service collection.
/// </summary>
public static class InfrastructureDependencies
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds infrastructure dependencies to the service collection.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            services.AddDatabase(configuration.GetConnectionString("neba-website")
                ?? throw new InvalidOperationException("Connection string 'neba-website' not found."));
            
            return services;
        }
    }
}