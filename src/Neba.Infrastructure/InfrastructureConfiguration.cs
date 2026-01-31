using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Neba.Application.Clock;
using Neba.Infrastructure.BackgroundJobs;
using Neba.Infrastructure.Clock;
using Neba.Infrastructure.Database;
using Neba.Infrastructure.Telemetry.Tracing;

namespace Neba.Infrastructure;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable CA1708 // Identifiers should differ by more than case

/// <summary>
/// Extension methods to add infrastructure dependencies to the service collection.
/// </summary>
public static class InfrastructureConfiguration
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds infrastructure dependencies to the service collection.
        /// </summary>
        /// <param name="config">The application configuration.</param>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddInfrastructure(IConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(config);

            // services.AddTracing(); // once we add a query handler or command handler uncomment this line

            // caching decorators can go here

            services.AddDatabase(config.GetConnectionString("neba-website")
                ?? throw new InvalidOperationException("Connection string 'neba-website' not found."));

            services.AddBackgroundJobs(config);

            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            services.AddSingleton<IStopwatchProvider, StopwatchProvider>();

            return services;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Uses infrastructure middleware in the application.
        /// </summary>
        /// <returns>The updated web application.</returns>
        public WebApplication UseInfrastructure()
        {
            app.UseBackgroundJobsDashboard();

            return app;
        }
    }
}