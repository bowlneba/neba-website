using Microsoft.Extensions.DependencyInjection;

using Neba.Application.Messaging;

namespace Neba.Application;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Classes should be static

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

            return services;
        }
    }
}