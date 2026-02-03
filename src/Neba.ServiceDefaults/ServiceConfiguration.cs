using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Neba.ServiceDefaults;
using Neba.ServiceDefaults.HealthChecks;
using Neba.ServiceDefaults.Telemetry;

#pragma warning disable IDE0130 // Namespace does not match folder structure
#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Private types or members should be static
namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
/// This project should be referenced by each service project in your solution.
/// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Extension members for <see cref="IHostApplicationBuilder"/> implementations.
    /// </summary>
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        /// <summary>
        /// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        public TBuilder AddServiceDefaults()
        {
            builder.ConfigureOpenTelemetry();

            builder.AddDefaultHealthChecks();

            builder.Services.AddServiceDiscovery();

            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                // Turn on resilience by default
                http.AddStandardResilienceHandler();

                // Turn on service discovery by default
                http.AddServiceDiscovery();
            });

            return builder;
        }
    }

    /// <summary>
    /// Extension members for <see cref="WebApplication"/>.
    /// </summary>
    extension(WebApplication app)
    {
        /// <summary>
        /// Maps default endpoints for health checks.
        /// </summary>
        /// <returns>The application for chaining.</returns>
        public WebApplication MapDefaultEndpoints()
        {
            app.MapDefaultHealthChecks();

            return app;
        }
    }
}